using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class PotionMixingMachineInteractable : MonoBehaviourPun, IInteractable, IItemAcceptor
    {
        private const string ResultPrefabName = "BasicMaterialItem";
        private const int MaxSlots = 3;

        private sealed class PotionSlot
        {
            public ItemObject Item;
            public ToolType PotionToolType = ToolType.None;

            public bool IsOccupied => Item != null;

            public void Clear()
            {
                Item = null;
                PotionToolType = ToolType.None;
            }
        }

        [SerializeField] private PotionMixingMachine _potionMixingMachine;
        [SerializeField] private MachineDoor _door;
        [SerializeField] private Transform[] _slotPoints = new Transform[MaxSlots];
        [SerializeField] private Transform _resultPoint;
        [SerializeField] private GameObject _resultPrefab;
        private float _pendingCraftingDuration;
        [SerializeField] private ActionTimer _actionTimer;
        [SerializeField] private RunningMotion _runningMotion;

        private PotionSlot[] _slots;
        private readonly List<ToolType> _loadedPotionsBuffer = new List<ToolType>(MaxSlots);
        private ItemObject _storedOutputItem;
        private CraftedMaterialType _pendingResultMaterial = CraftedMaterialType.Unknown;

        public bool IsInteracting => _actionTimer != null && _actionTimer.IsRunning;
        public Transform Transform => transform;

        private void Awake()
        {
            if (_potionMixingMachine == null)
            {
                _potionMixingMachine = GetComponent<PotionMixingMachine>();
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

            _slots = new PotionSlot[MaxSlots];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new PotionSlot();
            }
        }

        public void Interact(Transform interactor)
        {
            if (_potionMixingMachine == null || interactor == null)
            {
                return;
            }

            if (_actionTimer != null && _actionTimer.IsRunning)
            {
                return;
            }

            PlayerInteractionAbility interactionAbility = interactor.GetComponent<PlayerInteractionAbility>();
            if (interactionAbility == null)
            {
                return;
            }

            if (!IsDoorOpen())
            {
                HandleClosedDoorInteraction();
                return;
            }

            if (interactionAbility.CurrentHeldItem == null)
            {
                HandleOpenDoorEmptyHandInteraction(interactionAbility);
                return;
            }

            if (_storedOutputItem != null)
            {
                return;
            }

            int availableSlotIndex = GetFirstAvailableSlotIndex();
            if (availableSlotIndex < 0)
            {
                return;
            }

            TryInsertPotion(interactionAbility, interactionAbility.CurrentHeldItem, availableSlotIndex);
        }

        public void StopInteract()
        {
        }

        public bool CanAcceptItem(ItemObject item)
        {
            if (_potionMixingMachine == null)
                return false;

            if (_actionTimer != null && _actionTimer.IsRunning)
                return false;

            if (_storedOutputItem != null)
                return false;

            if (GetFirstAvailableSlotIndex() < 0)
                return false;

            return _potionMixingMachine.TryResolvePotionInput(item, out ToolType potionToolType)
                && _potionMixingMachine.CanInsertPotion(GetLoadedPotionToolTypes(), potionToolType);
        }

        #region Interaction Handlers

        private void HandleClosedDoorInteraction()
        {
            if (_storedOutputItem != null || !HasAnyStoredPotions())
            {
                OpenDoorAndSync();
                return;
            }

            List<ToolType> loadedPotions = GetLoadedPotionToolTypes();
            if (loadedPotions.Count >= 2 && _potionMixingMachine.CanMix(loadedPotions))
            {
                StartMixingProcess(loadedPotions);
                return;
            }

            OpenDoorAndSync();
        }

        private void HandleOpenDoorEmptyHandInteraction(PlayerInteractionAbility interactionAbility)
        {
            if (_storedOutputItem != null)
            {
                TryTakeOutput(interactionAbility);
                return;
            }

            if (!HasAnyStoredPotions())
            {
                return;
            }

            List<ToolType> loadedPotions = GetLoadedPotionToolTypes();
            bool canMix = loadedPotions.Count >= 2 && _potionMixingMachine.CanMix(loadedPotions);
            bool isFull = GetFirstAvailableSlotIndex() < 0;

            if (canMix || isFull)
            {
                CloseDoorAndSync();
                return;
            }

            TryTakeStoredInput(interactionAbility);
        }

        private void TryInsertPotion(PlayerInteractionAbility interactionAbility, ItemObject itemObject, int slotIndex)
        {
            if (!_potionMixingMachine.TryResolvePotionInput(itemObject, out ToolType potionToolType))
            {
                return;
            }

            if (!_potionMixingMachine.CanInsertPotion(GetLoadedPotionToolTypes(), potionToolType))
            {
                return;
            }

            if (!interactionAbility.TryReleaseHeldItem(itemObject))
            {
                return;
            }

            int itemViewId = GetPhotonViewId(itemObject);

            Transform slotTransform = GetSlotTransform(slotIndex);
            PlaceStoredItem(itemObject, slotTransform);
            SetStoredItemInteractionEnabled(itemObject, false);

            _slots[slotIndex].Item = itemObject;
            _slots[slotIndex].PotionToolType = potionToolType;

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_PotionInsert), RpcTarget.Others,
                    slotIndex, itemViewId, (int)potionToolType);
            }
        }

        private void StartMixingProcess(IReadOnlyList<ToolType> loadedPotions)
        {
            int playerId = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 0;
            CraftingResult result = _potionMixingMachine.TryMixPotions(loadedPotions, playerId);

            if (!result.Success || result.ResultMaterial == CraftedMaterialType.Unknown)
            {
                return;
            }

            _pendingResultMaterial = result.ResultMaterial;
            _pendingCraftingDuration = result.CraftingDuration;
            _door?.LockClosed();
            _runningMotion?.TryStart();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_PotionStartMixing), RpcTarget.Others,
                    (int)result.ResultMaterial, result.CraftingDuration);
            }

            if (_actionTimer == null)
            {
                OnMixingTimerComplete();
                return;
            }

            _actionTimer.TryStart(_pendingCraftingDuration, OnMixingTimerComplete);
        }

        private void OnMixingTimerComplete()
        {
            if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            {
                // 마스터에게 완료 처리 요청 (아이템 소유권이 마스터에 있으므로)
                photonView.RPC(nameof(RPC_PotionRequestCompletion), RpcTarget.MasterClient);
                return;
            }

            // 마스터이거나 오프라인: 직접 완료 처리
            CompleteMixingProcess();
        }

        private void CompleteMixingProcess()
        {
            _runningMotion?.StopMotion();
            ConsumeAllStoredInputs();

            if (_pendingResultMaterial == CraftedMaterialType.Unknown)
            {
                _door?.Unlock();

                if (PhotonNetwork.InRoom)
                {
                    photonView.RPC(nameof(RPC_PotionCompleteMixing), RpcTarget.Others, -1);
                }

                return;
            }

            Transform outputTransform = GetOutputTransform();
            GameObject resultObject = SpawnResult(_pendingResultMaterial, outputTransform.position, outputTransform.rotation);
            _pendingResultMaterial = CraftedMaterialType.Unknown;

            if (resultObject == null || !resultObject.TryGetComponent(out ItemObject resultItem))
            {
                _door?.Unlock();

                if (PhotonNetwork.InRoom)
                {
                    photonView.RPC(nameof(RPC_PotionCompleteMixing), RpcTarget.Others, -1);
                }

                return;
            }

            PlaceStoredItem(resultItem, outputTransform);
            SetStoredItemInteractionEnabled(resultItem, false);
            _storedOutputItem = resultItem;
            _door?.Unlock();

            if (PhotonNetwork.InRoom)
            {
                int resultViewId = GetPhotonViewId(resultItem);
                photonView.RPC(nameof(RPC_PotionCompleteMixing), RpcTarget.Others, resultViewId);
            }
        }

        private void TryTakeStoredInput(PlayerInteractionAbility interactionAbility)
        {
            int slotIndex = GetFirstOccupiedSlotIndex();
            if (slotIndex < 0)
            {
                return;
            }

            ItemObject storedItem = _slots[slotIndex].Item;
            if (storedItem == null || !storedItem.TryGetComponent(out IInteractable interactable))
            {
                return;
            }

            // 슬롯 상태를 먼저 정리 (비마스터의 소유권 대기 중에도 즉시 반영)
            _slots[slotIndex].Clear();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_PotionTakeInput), RpcTarget.Others, slotIndex);
            }

            // 반환값 무시: 비마스터는 false를 반환하지만 pending hold로 자동 처리됨
            // 소유권 획득 실패 시 아이템을 다시 인터랙션 가능 상태로 복원
            ItemObject itemToRestore = storedItem;
            interactionAbility.TryStartHoldFromExternal(interactable, () =>
            {
                SetStoredItemInteractionEnabled(itemToRestore, true);
            });
        }

        private void TryTakeOutput(PlayerInteractionAbility interactionAbility)
        {
            if (_storedOutputItem == null || !_storedOutputItem.TryGetComponent(out IInteractable interactable))
            {
                return;
            }

            ItemObject itemToRestore = _storedOutputItem;

            // 출력 상태를 먼저 정리 (비마스터의 소유권 대기 중에도 즉시 반영)
            _storedOutputItem = null;

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_PotionTakeOutput), RpcTarget.Others);
            }

            // 반환값 무시: 비마스터는 false를 반환하지만 pending hold로 자동 처리됨
            // 소유권 획득 실패 시 아이템을 다시 인터랙션 가능 상태로 복원
            interactionAbility.TryStartHoldFromExternal(interactable, () =>
            {
                SetStoredItemInteractionEnabled(itemToRestore, true);
            });
        }

        #endregion

        #region RPC Handlers

        [PunRPC]
        private void RPC_PotionRequestCompletion()
        {
            // 마스터 클라이언트만 완료 처리 실행
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            CompleteMixingProcess();
        }

        [PunRPC]
        private void RPC_PotionInsert(int slotIndex, int itemViewId, int potionToolType)
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
            _slots[slotIndex].PotionToolType = (ToolType)potionToolType;
        }

        [PunRPC]
        private void RPC_PotionStartMixing(int resultMaterial, float duration)
        {
            _pendingResultMaterial = (CraftedMaterialType)resultMaterial;
            _pendingCraftingDuration = duration;
            _door?.LockClosed();
            _runningMotion?.TryStart();

            // 원격 클라이언트는 타이머를 시각적으로만 실행 (완료 콜백 없음)
            _actionTimer?.TryStart(duration, () => { });
        }

        [PunRPC]
        private void RPC_PotionCompleteMixing(int resultItemViewId)
        {
            _runningMotion?.StopMotion();
            _actionTimer?.Cancel();

            // 슬롯 초기화 (아이템은 PhotonNetwork.Destroy로 이미 제거됨)
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Clear();
            }

            _pendingResultMaterial = CraftedMaterialType.Unknown;

            if (resultItemViewId >= 0)
            {
                PhotonView resultPV = PhotonView.Find(resultItemViewId);
                if (resultPV != null && resultPV.TryGetComponent(out ItemObject resultItem))
                {
                    Transform outputTransform = GetOutputTransform();
                    PlaceStoredItem(resultItem, outputTransform);
                    SetStoredItemInteractionEnabled(resultItem, false);
                    _storedOutputItem = resultItem;
                }
            }

            _door?.Unlock();
        }

        [PunRPC]
        private void RPC_PotionTakeInput(int slotIndex)
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
        }

        [PunRPC]
        private void RPC_PotionTakeOutput()
        {
            if (_storedOutputItem != null)
            {
                SetStoredItemInteractionEnabled(_storedOutputItem, true);
            }

            _storedOutputItem = null;
        }

        [PunRPC]
        private void RPC_PotionOpenDoor()
        {
            _door?.TryOpen();
        }

        [PunRPC]
        private void RPC_PotionCloseDoor()
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
                photonView.RPC(nameof(RPC_PotionOpenDoor), RpcTarget.Others);
            }
        }

        private void CloseDoorAndSync()
        {
            _door?.TryClose();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_PotionCloseDoor), RpcTarget.Others);
            }
        }

        #endregion

        #region Slot Queries

        private void ConsumeAllStoredInputs()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                ItemObject itemObject = _slots[i].Item;
                if (itemObject == null)
                {
                    _slots[i].Clear();
                    continue;
                }

                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.Destroy(itemObject.gameObject);
                }
                else
                {
                    Destroy(itemObject.gameObject);
                }

                _slots[i].Clear();
            }
        }

        private List<ToolType> GetLoadedPotionToolTypes()
        {
            _loadedPotionsBuffer.Clear();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied)
                {
                    _loadedPotionsBuffer.Add(_slots[i].PotionToolType);
                }
            }

            return _loadedPotionsBuffer;
        }

        private bool HasAnyStoredPotions()
        {
            return GetFirstOccupiedSlotIndex() >= 0;
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
            if (_slotPoints != null &&
                slotIndex >= 0 &&
                slotIndex < _slotPoints.Length &&
                _slotPoints[slotIndex] != null)
            {
                return _slotPoints[slotIndex];
            }

            return transform;
        }

        private Transform GetOutputTransform()
        {
            if (_resultPoint != null)
            {
                return _resultPoint;
            }

            if (_slotPoints != null && _slotPoints.Length > 0 && _slotPoints[0] != null)
            {
                return _slotPoints[0];
            }

            return transform;
        }

        private bool IsDoorOpen()
        {
            return _door == null || _door.IsOpen;
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

            PhotonView pv = itemObject.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine && PhotonNetwork.MasterClient != null)
            {
                pv.TransferOwnership(PhotonNetwork.MasterClient);
            }
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

        private GameObject SpawnResult(CraftedMaterialType resultMaterial, Vector3 position, Quaternion rotation)
        {
            if (PhotonNetwork.InRoom)
            {
                return PhotonNetwork.Instantiate(ResultPrefabName, position, rotation, 0, new object[] { (int)resultMaterial });
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
