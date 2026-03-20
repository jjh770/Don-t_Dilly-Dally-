using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class SterilizationMachineInteractable : MonoBehaviour, IInteractable
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
                if (_isBatchCompleted)
                {
                    TryTakeCompletedItem(interactionAbility);
                    return;
                }

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

            TryInsertItem(interactionAbility, interactionAbility.CurrentHeldItem, availableSlotIndex);
        }

        public void StopInteract()
        {
        }

        private void TryInsertItem(PlayerInteractionAbility interactionAbility, ItemObject itemObject, int slotIndex)
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

            if (!interactionAbility.TryReleaseHeldItem(itemObject, returnOwnershipToMaster: false))
            {
                return;
            }

            Transform slotTransform = GetSlotTransform(slotIndex);
            PlaceStoredItem(itemObject, slotTransform);
            SetStoredItemInteractionEnabled(itemObject, false);

            _slots[slotIndex].Item = itemObject;
            _slots[slotIndex].PendingResultMaterial = pendingResultMaterial;
        }

        private void StartSterilizationBatch()
        {
            if (!HasAnyStoredItems() || _isBatchCompleted)
            {
                return;
            }

            _door?.LockClosed();
            _runningMotion?.TryStart();

            if (_actionTimer == null)
            {
                CompleteSterilizationBatch();
                return;
            }

            _actionTimer.TryStart(_sterilizationDuration, CompleteSterilizationBatch);
        }

        private void CompleteSterilizationBatch()
        {
            _runningMotion?.StopMotion();

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
                    continue;
                }

                if (slot.Item is MixToolItem mixToolItem && slot.HasPendingToolResult)
                {
                    CraftedMaterialType resultMaterial = slot.PendingResultMaterial;
                    Transform slotTransform = GetSlotTransform(i);

                    if (PhotonNetwork.InRoom)
                    {
                        PhotonNetwork.Destroy(mixToolItem.gameObject);
                    }
                    else
                    {
                        Destroy(mixToolItem.gameObject);
                    }

                    slot.Clear();

                    GameObject resultObject = SpawnSterilizedResult(resultMaterial, slotTransform.position, slotTransform.rotation);
                    if (resultObject == null || !resultObject.TryGetComponent(out ItemObject resultItem))
                    {
                        continue;
                    }

                    PlaceStoredItem(resultItem, slotTransform);
                    SetStoredItemInteractionEnabled(resultItem, false);
                    slot.Item = resultItem;
                }
            }

            _isBatchCompleted = HasAnyStoredItems();
            _door?.Unlock();
        }

        private void TryTakeCompletedItem(PlayerInteractionAbility interactionAbility)
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

            if (!interactionAbility.TryStartHoldFromExternal(interactable))
            {
                return;
            }

            _slots[slotIndex].Clear();
            if (!HasAnyStoredItems())
            {
                _isBatchCompleted = false;
            }
        }

        private bool HasAnyStoredItems()
        {
            return GetFirstOccupiedSlotIndex() >= 0;
        }

        private void HandleClosedDoorInteraction()
        {
            if (_door == null)
            {
                return;
            }

            if (_isBatchCompleted || !HasAnyStoredItems())
            {
                _door.TryOpen();
                return;
            }

            StartSterilizationBatch();
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

        private Transform GetDefaultSlotTransform()
        {
            return transform;
        }

        private Transform GetSlotTransform(int slotIndex)
        {
            if (_traySlotPoints != null &&
                slotIndex >= 0 &&
                slotIndex < _traySlotPoints.Length &&
                _traySlotPoints[slotIndex] != null)
            {
                return _traySlotPoints[slotIndex];
            }

            return GetDefaultSlotTransform();
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

            PhotonView photonView = itemObject.GetComponent<PhotonView>();
            if (photonView != null && PhotonNetwork.MasterClient != null)
            {
                photonView.TransferOwnership(PhotonNetwork.MasterClient);
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
    }
}
