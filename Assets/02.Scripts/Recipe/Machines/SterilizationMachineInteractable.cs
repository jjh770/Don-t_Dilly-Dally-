using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class SterilizationMachineInteractable : MonoBehaviourPun, IInteractable, IItemAcceptor
    {
        private const string SterilizedResultPrefabName = "BasicMaterialItem";
        private const int MaxSlots = 4;

        private sealed class SterilizationSlot
        {
            public ItemObject Item;
            public CraftedMaterialType PendingResultMaterial = CraftedMaterialType.Unknown;

            public bool IsOccupied => Item != null;
            public bool HasPendingToolResult => PendingResultMaterial != CraftedMaterialType.Unknown;

            public void Clear()
            {
                Item = null;
                PendingResultMaterial = CraftedMaterialType.Unknown;
            }
        }

        [SerializeField] private SterilizationMachine _sterilizationMachine;
        [SerializeField] private MachineDoor _door;
        [SerializeField] private Transform[] _traySlotPoints = new Transform[MaxSlots];
        [SerializeField] private GameObject _resultPrefab;
        [SerializeField] private float _sterilizationDuration = 5f;
        [SerializeField] private ActionTimer _actionTimer;
        [SerializeField] private RunningMotion _runningMotion;

        private SterilizationSlot[] _slots;
        private bool _isBatchCompleted;

        public bool IsInteracting => _actionTimer != null && _actionTimer.IsRunning;
        public Transform Transform => transform;

        private void Awake()
        {
            if (_sterilizationMachine == null)
            {
                _sterilizationMachine = GetComponent<SterilizationMachine>();
            }

            if (_door == null)
            {
                _door = GetComponentInChildren<MachineDoor>(true);
            }

            if (_actionTimer == null)
            {
                _actionTimer = GetComponentInChildren<ActionTimer>(true);
            }

            if (_runningMotion == null)
            {
                _runningMotion = GetComponentInChildren<RunningMotion>(true);
            }

            _slots = new SterilizationSlot[MaxSlots];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new SterilizationSlot();
            }
        }

        public void Interact(Transform interactor)
        {
            if (_sterilizationMachine == null || interactor == null)
            {
                return;
            }

            if (_actionTimer != null && _actionTimer.IsRunning)
            {
                return;
            }

            IHeldItemInteractor heldItemInteractor = interactor.GetComponent<IHeldItemInteractor>();
            if (heldItemInteractor == null)
            {
                return;
            }

            if (!IsDoorOpen())
            {
                HandleClosedDoorInteraction();
                return;
            }

            if (heldItemInteractor.CurrentHeldItem == null)
            {
                HandleOpenDoorEmptyHandInteraction(heldItemInteractor);
                return;
            }

            if (_isBatchCompleted)
            {
                return;
            }

            int availableSlotIndex = GetFirstAvailableSlotIndex();
            if (availableSlotIndex < 0)
            {
                return;
            }

            TryInsertItem(heldItemInteractor, heldItemInteractor.CurrentHeldItem, availableSlotIndex);
        }

        public void StopInteract()
        {
        }

        public bool CanAcceptItem(ItemObject item)
        {
            if (_sterilizationMachine == null)
                return false;

            if (_actionTimer != null && _actionTimer.IsRunning)
                return false;

            if (_isBatchCompleted)
                return false;

            if (GetFirstAvailableSlotIndex() < 0)
                return false;

            if (item is TrayItem trayItem)
                return _sterilizationMachine.CanSterilizeTray(trayItem);

            if (item is MixToolItem mixToolItem)
            {
                int playerId = Photon.Pun.PhotonNetwork.LocalPlayer != null
                    ? Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber : 0;
                return _sterilizationMachine.TrySterilizeTool(mixToolItem.ToolType, playerId).Success;
            }

            return false;
        }

        #region Interaction Handlers

        private void TryInsertItem(IHeldItemInteractor heldItemInteractor, ItemObject itemObject, int slotIndex)
        {
            if (itemObject == null)
            {
                return;
            }

            CraftedMaterialType pendingResultMaterial = CraftedMaterialType.Unknown;

            if (itemObject is TrayItem trayItem)
            {
                if (!_sterilizationMachine.CanSterilizeTray(trayItem))
                {
                    return;
                }
            }
            else if (itemObject is MixToolItem mixToolItem)
            {
                int playerId = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 0;
                CraftingResult result = _sterilizationMachine.TrySterilizeTool(mixToolItem.ToolType, playerId);

                if (!result.Success || result.ResultMaterial == CraftedMaterialType.Unknown)
                {
                    return;
                }

                pendingResultMaterial = result.ResultMaterial;
            }
            else
            {
                return;
            }

            if (!heldItemInteractor.TryReleaseHeldItem(itemObject))
            {
                return;
            }

            int itemViewId = GetPhotonViewId(itemObject);

            Transform slotTransform = GetSlotTransform(slotIndex);
            PlaceStoredItem(itemObject, slotTransform);
            SetStoredItemInteractionEnabled(itemObject, false);

            _slots[slotIndex].Item = itemObject;
            _slots[slotIndex].PendingResultMaterial = pendingResultMaterial;

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilInsert), RpcTarget.Others,
                    slotIndex, itemViewId, (int)pendingResultMaterial);
            }
        }

        private void StartSterilizationBatch()
        {
            if (!HasAnyStoredItems() || _isBatchCompleted)
            {
                return;
            }

            _door?.LockClosed();
            _runningMotion?.TryStart();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilStartBatch), RpcTarget.Others, _sterilizationDuration);
            }

            if (_actionTimer == null)
            {
                OnSterilizationTimerComplete();
                return;
            }

            _actionTimer.TryStart(_sterilizationDuration, OnSterilizationTimerComplete);
        }

        private void OnSterilizationTimerComplete()
        {
            if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            {
                // 마스터에게 완료 처리 요청 (아이템 소유권이 마스터에 있으므로)
                photonView.RPC(nameof(RPC_SterilRequestCompletion), RpcTarget.MasterClient);
                return;
            }

            // 마스터이거나 오프라인: 직접 완료 처리
            CompleteSterilizationBatch();
        }

        private void CompleteSterilizationBatch()
        {
            _runningMotion?.StopMotion();

            // 결과 아이템의 ViewID를 수집하여 RPC로 전송
            int[] resultViewIds = new int[MaxSlots];
            for (int i = 0; i < resultViewIds.Length; i++)
            {
                resultViewIds[i] = -1;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                SterilizationSlot slot = _slots[i];
                if (!slot.IsOccupied)
                {
                    continue;
                }

                if (slot.Item is TrayItem trayItem)
                {
                    _sterilizationMachine.TrySterilizeTray(trayItem);
                    resultViewIds[i] = GetPhotonViewId(trayItem);
                    continue;
                }

                if (slot.Item is MixToolItem mixToolItem && slot.HasPendingToolResult)
                {
                    CraftedMaterialType resultMaterial = slot.PendingResultMaterial;
                    Transform slotTransform = GetSlotTransform(i);

                    ItemRecycleUtility.TryRecycle(mixToolItem);

                    slot.Clear();

                    GameObject resultObject = SpawnSterilizedResult(resultMaterial, slotTransform.position, slotTransform.rotation);
                    if (resultObject == null || !resultObject.TryGetComponent(out ItemObject resultItem))
                    {
                        continue;
                    }

                    PlaceStoredItem(resultItem, slotTransform);
                    SetStoredItemInteractionEnabled(resultItem, false);
                    slot.Item = resultItem;
                    resultViewIds[i] = GetPhotonViewId(resultItem);
                }
            }

            _isBatchCompleted = HasAnyStoredItems();
            _door?.Unlock();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilCompleteBatch), RpcTarget.Others, resultViewIds);
            }
        }

        private void TryTakeCompletedItem(IHeldItemInteractor heldItemInteractor)
        {
            int slotIndex = GetFirstOccupiedSlotIndex();
            if (slotIndex < 0)
            {
                _isBatchCompleted = false;
                return;
            }

            ItemObject storedItem = _slots[slotIndex].Item;
            if (storedItem == null)
            {
                return;
            }

            if (!storedItem.TryGetComponent(out IInteractable interactable))
            {
                return;
            }

            // 슬롯 상태를 먼저 정리 (비마스터의 소유권 대기 중에도 즉시 반영)
            _slots[slotIndex].Clear();
            bool hasRemaining = HasAnyStoredItems();
            if (!hasRemaining)
            {
                _isBatchCompleted = false;
            }

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilTakeItem), RpcTarget.Others, slotIndex);
            }

            // 반환값 무시: 비마스터는 false를 반환하지만 pending hold로 자동 처리됨
            // 소유권 획득 실패 시 아이템을 다시 인터랙션 가능 상태로 복원
            ItemObject itemToRestore = storedItem;
            heldItemInteractor.TryPickupInteractable(interactable, () =>
            {
                SetStoredItemInteractionEnabled(itemToRestore, true);
            });
        }

        private void HandleOpenDoorEmptyHandInteraction(IHeldItemInteractor heldItemInteractor)
        {
            if (_isBatchCompleted)
            {
                TryTakeCompletedItem(heldItemInteractor);
                return;
            }

            if (HasAnyStoredItems())
            {
                CloseDoorAndSync();
            }
        }

        private void HandleClosedDoorInteraction()
        {
            if (_door == null)
            {
                return;
            }

            if (_isBatchCompleted || !HasAnyStoredItems())
            {
                OpenDoorAndSync();
                return;
            }

            StartSterilizationBatch();
        }

        #endregion

        #region RPC Handlers

        [PunRPC]
        private void RPC_SterilRequestCompletion()
        {
            // 마스터 클라이언트만 완료 처리 실행
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            CompleteSterilizationBatch();
        }

        [PunRPC]
        private void RPC_SterilInsert(int slotIndex, int itemViewId, int pendingResultMaterial)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                return;
            }

            PhotonView itemPV = PhotonView.Find(itemViewId);
            if (itemPV == null || !itemPV.TryGetComponent(out ItemObject itemObject))
            {
                return;
            }

            Transform slotTransform = GetSlotTransform(slotIndex);
            PlaceStoredItem(itemObject, slotTransform);
            SetStoredItemInteractionEnabled(itemObject, false);

            _slots[slotIndex].Item = itemObject;
            _slots[slotIndex].PendingResultMaterial = (CraftedMaterialType)pendingResultMaterial;
        }

        [PunRPC]
        private void RPC_SterilStartBatch(float duration)
        {
            _door?.LockClosed();
            _runningMotion?.TryStart();

            // 원격 클라이언트는 타이머를 시각적으로만 실행 (완료 콜백 없음)
            _actionTimer?.TryStart(duration, () => { });
        }

        [PunRPC]
        private void RPC_SterilCompleteBatch(int[] resultViewIds)
        {
            _runningMotion?.StopMotion();
            _actionTimer?.Cancel();

            // 모든 슬롯 초기화 후 결과 아이템 재배치
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Clear();
            }

            if (resultViewIds != null)
            {
                for (int i = 0; i < resultViewIds.Length && i < _slots.Length; i++)
                {
                    if (resultViewIds[i] < 0)
                    {
                        continue;
                    }

                    PhotonView resultPV = PhotonView.Find(resultViewIds[i]);
                    if (resultPV == null || !resultPV.TryGetComponent(out ItemObject resultItem))
                    {
                        continue;
                    }

                    Transform slotTransform = GetSlotTransform(i);
                    PlaceStoredItem(resultItem, slotTransform);
                    SetStoredItemInteractionEnabled(resultItem, false);
                    _slots[i].Item = resultItem;
                }
            }

            _isBatchCompleted = HasAnyStoredItems();
            _door?.Unlock();
        }

        [PunRPC]
        private void RPC_SterilTakeItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                return;
            }

            ItemObject item = _slots[slotIndex].Item;
            if (item != null)
            {
                SetStoredItemInteractionEnabled(item, true);
            }

            _slots[slotIndex].Clear();

            if (!HasAnyStoredItems())
            {
                _isBatchCompleted = false;
            }
        }

        [PunRPC]
        private void RPC_SterilOpenDoor()
        {
            _door?.TryOpen();
        }

        [PunRPC]
        private void RPC_SterilCloseDoor()
        {
            _door?.TryClose();
        }

        #endregion

        #region Door Sync Helpers

        private void OpenDoorAndSync()
        {
            _door?.TryOpen();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilOpenDoor), RpcTarget.Others);
            }
        }

        private void CloseDoorAndSync()
        {
            _door?.TryClose();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilCloseDoor), RpcTarget.Others);
            }
        }

        #endregion

        #region Slot Queries

        private bool HasAnyStoredItems()
        {
            return GetFirstOccupiedSlotIndex() >= 0;
        }

        private bool IsDoorOpen()
        {
            return _door == null || _door.IsOpen;
        }

        private int GetFirstAvailableSlotIndex()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsOccupied)
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetFirstOccupiedSlotIndex()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        #region Utility

        private Transform GetSlotTransform(int slotIndex)
        {
            if (_traySlotPoints != null &&
                slotIndex >= 0 &&
                slotIndex < _traySlotPoints.Length &&
                _traySlotPoints[slotIndex] != null)
            {
                return _traySlotPoints[slotIndex];
            }

            return transform;
        }

        private void PlaceStoredItem(ItemObject itemObject, Transform slotTransform)
        {
            if (itemObject == null)
            {
                return;
            }

            if (itemObject.TryGetComponent(out HoldableItem holdableItem))
            {
                holdableItem.Place(slotTransform);
            }
            else
            {
                itemObject.transform.SetPositionAndRotation(slotTransform.position, slotTransform.rotation);
            }

            itemObject.transform.SetParent(slotTransform, true);

            NetworkItemOwnership.ReturnOwnershipToMaster(itemObject.GetComponent<PhotonView>());
        }

        private static void SetStoredItemInteractionEnabled(ItemObject itemObject, bool isEnabled)
        {
            if (itemObject == null)
            {
                return;
            }

            Collider[] colliders = itemObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                col.enabled = isEnabled;
            }

            HoldableItem holdable = itemObject.GetComponent<HoldableItem>();
            if (holdable != null)
            {
                holdable.SetStoredInContainer(!isEnabled);
            }

            if (isEnabled)
            {
                itemObject.transform.SetParent(null, true);
            }
        }

        private GameObject SpawnSterilizedResult(CraftedMaterialType resultMaterial, Vector3 position, Quaternion rotation)
        {
            if (PhotonNetwork.InRoom)
            {
                return PhotonNetwork.Instantiate(
                    SterilizedResultPrefabName,
                    position,
                    rotation,
                    0,
                    new object[] { (int)resultMaterial });
            }

            if (_resultPrefab == null)
            {
                return null;
            }

            GameObject spawnedObject = Instantiate(_resultPrefab, position, rotation);
            if (spawnedObject.TryGetComponent(out BasicMaterialItem basicMaterialItem))
            {
                basicMaterialItem.Initialize(resultMaterial);
            }

            return spawnedObject;
        }

        private static int GetPhotonViewId(ItemObject itemObject)
        {
            if (itemObject == null)
            {
                return -1;
            }

            PhotonView pv = itemObject.GetComponent<PhotonView>();
            return pv != null ? pv.ViewID : -1;
        }

        #endregion
    }
}
