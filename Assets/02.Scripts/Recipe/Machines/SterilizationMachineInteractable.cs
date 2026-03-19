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
        [SerializeField] private Transform[] _traySlotPoints = new Transform[MaxSlots];
        [SerializeField] private float _traySterilizationDuration = 5f;
        [SerializeField] private float _toolSterilizationDuration = 5f;
        [SerializeField] private WorldActionTimer _traySterilizationTimer;

        private SterilizationSlot[] _slots;
        private bool _isBatchCompleted;

        public bool IsInteracting => _traySterilizationTimer != null && _traySterilizationTimer.IsRunning;
        public Transform Transform => transform;

        private void Awake()
        {
            if (_sterilizationMachine == null)
                _sterilizationMachine = GetComponent<SterilizationMachine>();

            if (_traySterilizationTimer == null)
                _traySterilizationTimer = GetComponentInChildren<WorldActionTimer>(true);

            _slots = new SterilizationSlot[MaxSlots];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new SterilizationSlot();
            }
        }

        public void Interact(Transform interactor)
        {
            if (_sterilizationMachine == null || interactor == null)
                return;

            if (_traySterilizationTimer != null && _traySterilizationTimer.IsRunning)
                return;

            PlayerInteractionAbility interactionAbility = interactor.GetComponent<PlayerInteractionAbility>();
            if (interactionAbility == null)
                return;

            if (interactionAbility.CurrentHeldItem == null)
            {
                if (_isBatchCompleted)
                {
                    TryTakeCompletedItem(interactionAbility);
                    return;
                }

                if (HasAnyStoredItems())
                {
                    StartSterilizationBatch();
                }

                return;
            }

            if (_isBatchCompleted)
                return;

            int availableSlotIndex = GetFirstAvailableSlotIndex();
            if (availableSlotIndex < 0)
                return;

            if (interactionAbility.CurrentHeldItem is TrayItem trayItem)
            {
                TryInsertTray(interactionAbility, trayItem, availableSlotIndex);
                return;
            }

            if (interactionAbility.CurrentHeldItem is MixToolItem mixToolItem)
            {
                TryInsertTool(interactionAbility, mixToolItem, availableSlotIndex);
            }
        }

        public void StopInteract()
        {
        }

        private void TryInsertTray(
            PlayerInteractionAbility interactionAbility,
            TrayItem trayItem,
            int slotIndex)
        {
            if (!_sterilizationMachine.CanSterilizeTray(trayItem))
                return;

            if (!interactionAbility.TryReleaseHeldItem(trayItem, returnOwnershipToMaster: false))
                return;

            Transform slotTransform = GetSlotTransform(slotIndex);
            PlaceStoredItem(trayItem, slotTransform);
            SetStoredItemInteractionEnabled(trayItem, false);

            _slots[slotIndex].Item = trayItem;
            _slots[slotIndex].PendingResultMaterial = CraftedMaterialType.Unknown;
        }

        private void TryInsertTool(
            PlayerInteractionAbility interactionAbility,
            MixToolItem mixToolItem,
            int slotIndex)
        {
            int playerId = PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : 0;

            CraftingAttemptResult result =
                _sterilizationMachine.TrySterilizeTool(mixToolItem.ToolType, playerId);

            if (!result.Success || result.ResultMaterial == CraftedMaterialType.Unknown)
                return;

            if (!interactionAbility.TryReleaseHeldItem(mixToolItem, returnOwnershipToMaster: false))
                return;

            Transform slotTransform = GetSlotTransform(slotIndex);
            PlaceStoredItem(mixToolItem, slotTransform);
            SetStoredItemInteractionEnabled(mixToolItem, false);

            _slots[slotIndex].Item = mixToolItem;
            _slots[slotIndex].PendingResultMaterial = result.ResultMaterial;
        }

        private void StartSterilizationBatch()
        {
            if (!HasAnyStoredItems() || _isBatchCompleted)
                return;

            if (_traySterilizationTimer == null)
            {
                CompleteSterilizationBatch();
                return;
            }

            _traySterilizationTimer.TryStart(
                GetSterilizationDuration(),
                CompleteSterilizationBatch);
        }

        private float GetSterilizationDuration()
        {
            bool hasTray = false;
            bool hasTool = false;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsOccupied)
                    continue;

                if (_slots[i].Item is TrayItem)
                    hasTray = true;
                else if (_slots[i].Item is MixToolItem)
                    hasTool = true;
            }

            if (hasTray && hasTool)
                return Mathf.Max(_traySterilizationDuration, _toolSterilizationDuration);

            if (hasTool)
                return _toolSterilizationDuration;

            return _traySterilizationDuration;
        }

        private void CompleteSterilizationBatch()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                SterilizationSlot slot = _slots[i];
                if (!slot.IsOccupied)
                    continue;

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

                    GameObject resultObject = SpawnSterilizedResult(
                        resultMaterial,
                        slotTransform.position,
                        slotTransform.rotation);

                    if (resultObject == null || !resultObject.TryGetComponent(out ItemObject resultItem))
                        continue;

                    PlaceStoredItem(resultItem, slotTransform);
                    SetStoredItemInteractionEnabled(resultItem, false);
                    slot.Item = resultItem;
                }
            }

            _isBatchCompleted = HasAnyStoredItems();
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
                return;

            if (!storedItem.TryGetComponent(out IInteractable interactable))
                return;

            if (!interactionAbility.TryStartHoldFromExternal(interactable))
                return;

            _slots[slotIndex].Clear();
            if (!HasAnyStoredItems())
                _isBatchCompleted = false;
        }

        private bool HasAnyStoredItems()
        {
            return GetFirstOccupiedSlotIndex() >= 0;
        }

        private int GetFirstAvailableSlotIndex()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsOccupied)
                    return i;
            }

            return -1;
        }

        private int GetFirstOccupiedSlotIndex()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied)
                    return i;
            }

            return -1;
        }

        private Transform GetSlotTransform()
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

            return GetSlotTransform();
        }

        private void PlaceStoredItem(ItemObject itemObject, Transform slotTransform)
        {
            if (itemObject == null)
                return;

            if (itemObject.TryGetComponent(out HoldableItem holdableItem))
            {
                holdableItem.Place(slotTransform);
            }
            else
            {
                itemObject.transform.SetPositionAndRotation(slotTransform.position, slotTransform.rotation);
            }

            PhotonView photonView = itemObject.GetComponent<PhotonView>();
            if (photonView != null && PhotonNetwork.MasterClient != null)
            {
                photonView.TransferOwnership(PhotonNetwork.MasterClient);
            }
        }

        private static void SetStoredItemInteractionEnabled(ItemObject itemObject, bool isEnabled)
        {
            if (itemObject == null)
                return;

            Collider[] colliders = itemObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                col.enabled = isEnabled;
            }
        }

        private static GameObject SpawnSterilizedResult(
            CraftedMaterialType resultMaterial,
            Vector3 position,
            Quaternion rotation)
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

            GameObject prefab = Resources.Load<GameObject>(SterilizedResultPrefabName);
            if (prefab == null)
                return null;

            GameObject spawnedObject = Instantiate(prefab, position, rotation);
            if (spawnedObject.TryGetComponent(out BasicMaterialItem basicMaterialItem))
            {
                basicMaterialItem.Initialize(resultMaterial);
            }

            return spawnedObject;
        }
    }
}
