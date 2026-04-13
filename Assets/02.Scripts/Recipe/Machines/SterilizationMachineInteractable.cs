using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider), typeof(PhotonView))]
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
        private MachineOperationController _operationController;

        public bool IsInteracting => _operationController != null && _operationController.IsRunning;
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

            _operationController = new MachineOperationController(
                _door,
                _actionTimer,
                _runningMotion,
                SFXKey.SterilizerInProgress,
                SFXKey.SterilizerOpen,
                SFXKey.SterilizerClose,
                SFXKey.SterilizerComplete);

            _slots = new SterilizationSlot[MaxSlots];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new SterilizationSlot();
            }
        }

        private void OnDisable()
        {
            _operationController?.StopLoop();
        }

        public void Interact(Transform interactor)
        {
            if (_sterilizationMachine == null || interactor == null)
            {
                return;
            }

            if (IsInteracting)
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

            if (IsInteracting)
                return false;

            if (_isBatchCompleted)
                return false;

            if (GetFirstAvailableSlotIndex() < 0)
                return false;

            if (item is TrayItem trayItem)
                return _sterilizationMachine.CanSterilizeTray(trayItem);

            if (item is MixToolItem mixToolItem)
            {
                int playerId = PhotonNetwork.LocalPlayer != null
                    ? PhotonNetwork.LocalPlayer.ActorNumber : 0;
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

            int itemViewId = itemObject.ViewId;

            Transform slotTransform = GetSlotTransform(slotIndex);
            MachineStoredItemUtility.StoreInMachine(itemObject, slotTransform);

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

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilStartBatch), RpcTarget.Others, _sterilizationDuration);
            }

            _operationController.StartLocal(_sterilizationDuration, OnSterilizationTimerComplete);
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
            _operationController.CompleteLocal();

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
                    resultViewIds[i] = trayItem.ViewId;
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

                    MachineStoredItemUtility.StoreInMachine(resultItem, slotTransform);
                    slot.Item = resultItem;
                    resultViewIds[i] = resultItem.ViewId;
                }
            }

            _isBatchCompleted = HasAnyStoredItems();
            _operationController.UnlockDoor();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilCompleteBatch), RpcTarget.Others, resultViewIds);
            }

            if (_isBatchCompleted)
            {
                _operationController.PlayComplete();
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

            MachineStoredItemUtility.PrepareForPickup(storedItem);

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilTakeItem), RpcTarget.Others, slotIndex);
            }

            ItemObject itemToRestore = storedItem;
            int capturedSlotIndex = slotIndex;
            heldItemInteractor.TryPickupInteractable(interactable, () =>
            {
                RollbackTakeItem(itemToRestore, capturedSlotIndex);
            });
        }

        private void RollbackTakeItem(ItemObject item, int slotIndex)
        {
            if (item == null || slotIndex < 0 || slotIndex >= _slots.Length)
                return;

            // 다른 플레이어가 이미 집었으면 롤백하지 않음
            if (item.TryGetComponent(out HoldableItem holdable) && holdable.IsInteracting)
                return;

            Transform slotTransform = GetSlotTransform(slotIndex);
            MachineStoredItemUtility.StoreInMachine(item, slotTransform);
            _slots[slotIndex].Item = item;
            _isBatchCompleted = true;

            if (PhotonNetwork.InRoom)
            {
                int viewId = item.ViewId;
                photonView.RPC(nameof(RPC_SterilRollbackTakeItem), RpcTarget.Others, slotIndex, viewId);
            }
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
            MachineStoredItemUtility.StoreInMachine(itemObject, slotTransform);

            _slots[slotIndex].Item = itemObject;
            _slots[slotIndex].PendingResultMaterial = (CraftedMaterialType)pendingResultMaterial;
        }

        [PunRPC]
        private void RPC_SterilStartBatch(float duration)
        {
            _operationController.StartRemote(duration);
        }

        [PunRPC]
        private void RPC_SterilCompleteBatch(int[] resultViewIds)
        {
            _operationController.CompleteRemote();

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
                    MachineStoredItemUtility.StoreInMachine(resultItem, slotTransform);
                    _slots[i].Item = resultItem;
                }
            }

            _isBatchCompleted = HasAnyStoredItems();
            _operationController.UnlockDoor();

            if (_isBatchCompleted)
            {
                _operationController.PlayComplete();
            }
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
                MachineStoredItemUtility.PrepareForPickup(item);
            }

            _slots[slotIndex].Clear();

            if (!HasAnyStoredItems())
            {
                _isBatchCompleted = false;
            }
        }

        [PunRPC]
        private void RPC_SterilRollbackTakeItem(int slotIndex, int itemViewId)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
                return;

            PhotonView itemPV = PhotonView.Find(itemViewId);
            if (itemPV == null || !itemPV.TryGetComponent(out ItemObject itemObject))
                return;

            // 다른 플레이어가 이미 집었으면 롤백하지 않음
            if (itemObject.TryGetComponent(out HoldableItem holdable) && holdable.IsInteracting)
                return;

            Transform slotTransform = GetSlotTransform(slotIndex);
            MachineStoredItemUtility.StoreInMachine(itemObject, slotTransform);
            _slots[slotIndex].Item = itemObject;
            _isBatchCompleted = true;
        }

        [PunRPC]
        private void RPC_SterilOpenDoor()
        {
            _operationController?.TryOpenDoor();
        }

        [PunRPC]
        private void RPC_SterilCloseDoor()
        {
            _operationController?.TryCloseDoor();
        }

        #endregion

        #region Door Sync Helpers

        private void OpenDoorAndSync()
        {
            if (!_operationController.TryOpenDoor())
            {
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_SterilOpenDoor), RpcTarget.Others);

            }
        }

        private void CloseDoorAndSync()
        {
            if (!_operationController.TryCloseDoor())
            {
                return;
            }

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
            return _operationController == null || _operationController.IsDoorOpen;
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

        #endregion
    }
}
