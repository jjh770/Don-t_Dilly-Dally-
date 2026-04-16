using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider), typeof(PhotonView))]
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
        private float _pendingCraftingDuration;
        [SerializeField] private ActionTimer _actionTimer;
        [SerializeField] private RunningMotion _runningMotion;

        private PotionSlot[] _slots;
        private readonly List<ToolType> _loadedPotionsBuffer = new List<ToolType>(MaxSlots);
        private ItemObject _storedOutputItem;
        private CraftedMaterialType _pendingResultMaterial = CraftedMaterialType.Unknown;
        private MachineOperationController _operationController;
        private bool _isCompletionPending;

        public bool IsInteracting => _operationController != null && (_operationController.IsRunning || _isCompletionPending);
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

            _operationController = new MachineOperationController(
                _door,
                _actionTimer,
                _runningMotion,
                SFXKey.PotionMixerInProgress,
                SFXKey.PotionMixerOpen,
                SFXKey.PotionMixerClose,
                SFXKey.PotionMixerComplete);

            _slots = new PotionSlot[MaxSlots];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new PotionSlot();
            }
        }

        private void OnDisable()
        {
            _operationController?.StopLoop();
        }

        public void Interact(Transform interactor)
        {
            if (_potionMixingMachine == null || interactor == null)
            {
                return;
            }

            if (IsInteracting)
            {
                return;
            }

            ClearDetachedPotionState();

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

            if (_storedOutputItem != null)
            {
                return;
            }

            int availableSlotIndex = GetFirstAvailableSlotIndex();
            if (availableSlotIndex < 0)
            {
                return;
            }

            TryInsertPotion(heldItemInteractor, heldItemInteractor.CurrentHeldItem, availableSlotIndex);
        }

        public void StopInteract()
        {
        }

        public bool CanAcceptItem(ItemObject item)
        {
            if (_potionMixingMachine == null)
                return false;

            ClearDetachedPotionState();

            if (IsInteracting)
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

        private void HandleOpenDoorEmptyHandInteraction(IHeldItemInteractor heldItemInteractor)
        {
            if (_storedOutputItem != null)
            {
                TryTakeOutput(heldItemInteractor);
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

            TryTakeStoredInput(heldItemInteractor);
        }

        private void TryInsertPotion(IHeldItemInteractor heldItemInteractor, ItemObject itemObject, int slotIndex)
        {
            if (!_potionMixingMachine.TryResolvePotionInput(itemObject, out ToolType potionToolType))
            {
                return;
            }

            if (!_potionMixingMachine.CanInsertPotion(GetLoadedPotionToolTypes(), potionToolType))
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
            _slots[slotIndex].PotionToolType = potionToolType;

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_PotionInsert), RpcTarget.Others,
                    slotIndex, itemViewId, (int)potionToolType);
            }
        }

        private void StartMixingProcess(IReadOnlyList<ToolType> loadedPotions)
        {
            if (_isCompletionPending)
            {
                return;
            }

            int playerId = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 0;
            CraftingResult result = _potionMixingMachine.TryMixPotions(loadedPotions, playerId);

            if (!result.Success || result.ResultMaterial == CraftedMaterialType.Unknown)
            {
                return;
            }

            _pendingResultMaterial = result.ResultMaterial;
            _pendingCraftingDuration = result.CraftingDuration;

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_PotionStartMixing), RpcTarget.Others,
                    (int)result.ResultMaterial, result.CraftingDuration);
            }

            _operationController.StartLocal(_pendingCraftingDuration, OnMixingTimerComplete);
        }

        private void OnMixingTimerComplete()
        {
            _isCompletionPending = true;

            if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            {
                // 마스터에게 완료 처리 요청 (아이템 소유권이 마스터에 있으므로)
                photonView.RPC(nameof(RPC_PotionRequestCompletion), RpcTarget.MasterClient);
                return;
            }

            CompleteMixingProcess();
        }

        private void CompleteMixingProcess()
        {
            _operationController.CompleteLocal();
            ConsumeAllStoredInputs();

            if (_pendingResultMaterial == CraftedMaterialType.Unknown)
            {
                _isCompletionPending = false;
                _operationController.UnlockDoor();

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
                _isCompletionPending = false;
                _operationController.UnlockDoor();

                if (PhotonNetwork.InRoom)
                {
                    photonView.RPC(nameof(RPC_PotionCompleteMixing), RpcTarget.Others, -1);
                }

                return;
            }

            MachineStoredItemUtility.StoreInMachine(resultItem, outputTransform);
            _storedOutputItem = resultItem;
            _isCompletionPending = false;
            _operationController.UnlockDoor();

            if (PhotonNetwork.InRoom)
            {
                int resultViewId = resultItem.ViewId;
                photonView.RPC(nameof(RPC_PotionCompleteMixing), RpcTarget.Others, resultViewId);
            }

            _operationController.PlayComplete();

        }

        private void TryTakeStoredInput(IHeldItemInteractor heldItemInteractor)
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

            heldItemInteractor.TryPickupInteractable(
                interactable,
                onBeforeHold: () =>
                {
                    if (_slots[slotIndex].Item != storedItem)
                    {
                        return false;
                    }

                    _slots[slotIndex].Clear();
                    MachineStoredItemUtility.PrepareForPickup(storedItem);

                    if (PhotonNetwork.InRoom)
                    {
                        photonView.RPC(nameof(RPC_PotionTakeInput), RpcTarget.Others, slotIndex);
                    }

                    return true;
                });
        }

        private void TryTakeOutput(IHeldItemInteractor heldItemInteractor)
        {
            ClearDetachedPotionState();

            if (_storedOutputItem == null || !_storedOutputItem.TryGetComponent(out IInteractable interactable))
            {
                return;
            }

            ItemObject itemToRestore = _storedOutputItem;

            heldItemInteractor.TryPickupInteractable(
                interactable,
                onBeforeHold: () =>
                {
                    if (_storedOutputItem != itemToRestore)
                    {
                        return false;
                    }

                    _storedOutputItem = null;
                    MachineStoredItemUtility.PrepareForPickup(itemToRestore);

                    if (PhotonNetwork.InRoom)
                    {
                        photonView.RPC(nameof(RPC_PotionTakeOutput), RpcTarget.Others);
                    }

                    return true;
                });
        }

        private void RollbackTakeInput(ItemObject item, int slotIndex, ToolType potionToolType)
        {
            if (item == null)
                return;

            // 다른 플레이어가 이미 집었으면 롤백하지 않음
            if (item.TryGetComponent(out HoldableItem holdable) && holdable.IsInteracting)
                return;

            Transform slotTransform = GetSlotTransform(slotIndex);
            MachineStoredItemUtility.StoreInMachine(item, slotTransform);
            _slots[slotIndex].Item = item;
            _slots[slotIndex].PotionToolType = potionToolType;

            if (PhotonNetwork.InRoom)
            {
                int viewId = item.ViewId;
                photonView.RPC(nameof(RPC_PotionRollbackTakeInput), RpcTarget.Others, slotIndex, viewId, (int)potionToolType);
            }
        }

        private void RollbackTakeOutput(ItemObject item)
        {
            if (item == null)
                return;

            // 다른 플레이어가 이미 집었으면 롤백하지 않음
            if (item.TryGetComponent(out HoldableItem holdable) && holdable.IsInteracting)
                return;

            Transform outputTransform = GetOutputTransform();
            MachineStoredItemUtility.StoreInMachine(item, outputTransform);
            _storedOutputItem = item;

            if (PhotonNetwork.InRoom)
            {
                int viewId = item.ViewId;
                photonView.RPC(nameof(RPC_PotionRollbackTakeOutput), RpcTarget.Others, viewId);
            }
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
            MachineStoredItemUtility.StoreInMachine(itemObject, slotTransform);

            _slots[slotIndex].Item = itemObject;
            _slots[slotIndex].PotionToolType = (ToolType)potionToolType;
        }

        [PunRPC]
        private void RPC_PotionStartMixing(int resultMaterial, float duration)
        {
            _isCompletionPending = false;
            _pendingResultMaterial = (CraftedMaterialType)resultMaterial;
            _pendingCraftingDuration = duration;
            _operationController.StartRemote(duration, () => _isCompletionPending = true);
        }

        [PunRPC]
        private void RPC_PotionCompleteMixing(int resultItemViewId)
        {
            _operationController.CompleteRemote();
            _isCompletionPending = false;

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
                    MachineStoredItemUtility.StoreInMachine(resultItem, outputTransform);
                    _storedOutputItem = resultItem;
                }
            }

            _operationController.UnlockDoor();

            if (resultItemViewId >= 0)
            {
                _operationController.PlayComplete();
            }
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
                MachineStoredItemUtility.PrepareForPickup(item);
            }

            _slots[slotIndex].Clear();
        }

        [PunRPC]
        private void RPC_PotionTakeOutput()
        {
            if (_storedOutputItem != null)
            {
                MachineStoredItemUtility.PrepareForPickup(_storedOutputItem);
            }

            _storedOutputItem = null;
        }

        [PunRPC]
        private void RPC_PotionRollbackTakeInput(int slotIndex, int itemViewId, int potionToolType)
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
            _slots[slotIndex].PotionToolType = (ToolType)potionToolType;
        }

        [PunRPC]
        private void RPC_PotionRollbackTakeOutput(int itemViewId)
        {
            PhotonView itemPV = PhotonView.Find(itemViewId);
            if (itemPV == null || !itemPV.TryGetComponent(out ItemObject itemObject))
                return;

            // 다른 플레이어가 이미 집었으면 롤백하지 않음
            if (itemObject.TryGetComponent(out HoldableItem holdable) && holdable.IsInteracting)
                return;

            Transform outputTransform = GetOutputTransform();
            MachineStoredItemUtility.StoreInMachine(itemObject, outputTransform);
            _storedOutputItem = itemObject;
        }

        [PunRPC]
        private void RPC_PotionOpenDoor()
        {
            _operationController?.TryOpenDoor();
        }

        [PunRPC]
        private void RPC_PotionCloseDoor()
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
                photonView.RPC(nameof(RPC_PotionOpenDoor), RpcTarget.Others);
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

                ItemRecycleUtility.TryRecycle(itemObject);

                _slots[i].Clear();
            }
        }

        private List<ToolType> GetLoadedPotionToolTypes()
        {
            ClearDetachedPotionState();

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
            ClearDetachedPotionState();
            return GetFirstOccupiedSlotIndex() >= 0;
        }

        private int GetFirstAvailableSlotIndex()
        {
            ClearDetachedPotionState();

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
            ClearDetachedPotionState();

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ClearDetachedPotionState()
        {
            if (_slots == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                PotionSlot slot = _slots[i];
                ItemObject item = slot.Item;
                if (item == null)
                {
                    slot.Clear();
                    continue;
                }

                if (item.TryGetComponent(out HoldableItem holdable) &&
                    !holdable.IsStoredInContainer &&
                    item.transform.parent != GetSlotTransform(i))
                {
                    slot.Clear();
                }
            }

            if (_storedOutputItem != null &&
                _storedOutputItem.TryGetComponent(out HoldableItem outputHoldable) &&
                !outputHoldable.IsStoredInContainer &&
                _storedOutputItem.transform.parent != GetOutputTransform())
            {
                _storedOutputItem = null;
            }
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
            return _operationController == null || _operationController.IsDoorOpen;
        }

        private GameObject SpawnResult(CraftedMaterialType resultMaterial, Vector3 position, Quaternion rotation)
        {
            if (!PhotonNetwork.InRoom)
            {
                return null;
            }

            return PhotonNetwork.InstantiateRoomObject(ResultPrefabName, position, rotation, 0, new object[] { (int)resultMaterial });
        }

        #endregion
    }
}
