using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class PotionMixingMachineInteractable : MonoBehaviour, IInteractable
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
        [SerializeField] private float _mixingDuration = 5f;
        [SerializeField] private ActionTimer _actionTimer;
        [SerializeField] private RunningMotion _runningMotion;

        private PotionSlot[] _slots;
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

        private void HandleClosedDoorInteraction()
        {
            if (_storedOutputItem != null || !HasAnyStoredPotions())
            {
                _door?.TryOpen();
                return;
            }

            List<ToolType> loadedPotions = GetLoadedPotionToolTypes();
            if (loadedPotions.Count >= 2 && _potionMixingMachine.CanMix(loadedPotions))
            {
                StartMixingProcess(loadedPotions);
                return;
            }

            _door?.TryOpen();
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
                _door?.TryClose();
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

            if (!interactionAbility.TryReleaseHeldItem(itemObject, returnOwnershipToMaster: false))
            {
                return;
            }

            Transform slotTransform = GetSlotTransform(slotIndex);
            PlaceStoredItem(itemObject, slotTransform);
            SetStoredItemInteractionEnabled(itemObject, false);

            _slots[slotIndex].Item = itemObject;
            _slots[slotIndex].PotionToolType = potionToolType;
        }

        private void StartMixingProcess(IReadOnlyList<ToolType> loadedPotions)
        {
            int playerId = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 0;
            CraftingAttemptResult result = _potionMixingMachine.TryMixPotions(loadedPotions, playerId);

            if (!result.Success || result.ResultMaterial == CraftedMaterialType.Unknown)
            {
                return;
            }

            _pendingResultMaterial = result.ResultMaterial;
            _door?.LockClosed();
            _runningMotion?.TryStart();

            if (_actionTimer == null)
            {
                CompleteMixingProcess();
                return;
            }

            _actionTimer.TryStart(_mixingDuration, CompleteMixingProcess);
        }

        private void CompleteMixingProcess()
        {
            _runningMotion?.StopMotion();
            ConsumeAllStoredInputs();

            if (_pendingResultMaterial == CraftedMaterialType.Unknown)
            {
                _door?.Unlock();
                return;
            }

            Transform outputTransform = GetOutputTransform();
            GameObject resultObject = SpawnResult(_pendingResultMaterial, outputTransform.position, outputTransform.rotation);
            _pendingResultMaterial = CraftedMaterialType.Unknown;

            if (resultObject == null || !resultObject.TryGetComponent(out ItemObject resultItem))
            {
                _door?.Unlock();
                return;
            }

            PlaceStoredItem(resultItem, outputTransform);
            SetStoredItemInteractionEnabled(resultItem, false);
            _storedOutputItem = resultItem;
            _door?.Unlock();
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

            if (!interactionAbility.TryStartHoldFromExternal(interactable))
            {
                return;
            }

            _slots[slotIndex].Clear();
        }

        private void TryTakeOutput(PlayerInteractionAbility interactionAbility)
        {
            if (_storedOutputItem == null || !_storedOutputItem.TryGetComponent(out IInteractable interactable))
            {
                return;
            }

            if (!interactionAbility.TryStartHoldFromExternal(interactable))
            {
                return;
            }

            _storedOutputItem = null;
        }

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
            List<ToolType> loadedPotions = new List<ToolType>();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied)
                {
                    loadedPotions.Add(_slots[i].PotionToolType);
                }
            }

            return loadedPotions;
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
    }
}
