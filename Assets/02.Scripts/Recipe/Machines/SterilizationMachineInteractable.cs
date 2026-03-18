using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class SterilizationMachineInteractable : MonoBehaviour, IInteractable
    {
        private const string SterilizedResultPrefabName = "BasicMaterialItem";
        private const float StoredItemSlotTolerance = 0.05f;
        private const int MaxTraySlots = 4;

        [SerializeField] private SterilizationMachine _sterilizationMachine;
        [SerializeField] private Transform[] _traySlotPoints = new Transform[MaxTraySlots];
        [SerializeField] private float _traySterilizationDuration = 5f;
        [SerializeField] private Transform _toolSlotPoint;
        [SerializeField] private float _toolSterilizationDuration = 5f;
        [SerializeField] private WorldActionTimer _traySterilizationTimer;

        private ItemObject _storedOutputItem;
        private readonly List<TrayItem> _loadedTrayItems = new();
        private bool _areLoadedTraysSterilized;
        private CraftedMaterialType _processingToolResultMaterial = CraftedMaterialType.Unknown;

        public bool IsInteracting => _traySterilizationTimer != null && _traySterilizationTimer.IsRunning;
        public Transform Transform => transform;

        private void Awake()
        {
            if (_sterilizationMachine == null)
                _sterilizationMachine = GetComponent<SterilizationMachine>();

            if (_traySterilizationTimer == null)
                _traySterilizationTimer = GetComponentInChildren<WorldActionTimer>(true);
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
                if (_areLoadedTraysSterilized)
                {
                    TryTakeCompletedTray(interactionAbility);
                    return;
                }

                if (_loadedTrayItems.Count > 0)
                {
                    StartTraySterilizationBatch();
                    return;
                }

                TryTakeCompletedItem(interactionAbility);
                return;
            }

            if (_areLoadedTraysSterilized)
                return;

            ItemObject heldItem = interactionAbility.CurrentHeldItem;
            if (GetStoredOutputItem() != null)
                return;

            if (heldItem is TrayItem trayItem)
            {
                TryInsertTray(interactionAbility, trayItem);
                return;
            }

            if (_loadedTrayItems.Count > 0)
                return;

            if (heldItem is MixToolItem mixToolItem)
            {
                TrySterilizeToolItem(interactionAbility, mixToolItem);
            }
        }

        public void StopInteract()
        {
        }

        private ItemObject GetStoredOutputItem()
        {
            if (_storedOutputItem == null)
                return null;

            if (_storedOutputItem.NetworkOwnership != null && _storedOutputItem.NetworkOwnership.IsHeld)
            {
                _storedOutputItem = null;
                return null;
            }

            Transform slotTransform = _toolSlotPoint != null ? _toolSlotPoint : GetSlotTransform();
            if (slotTransform != null)
            {
                float sqrDistance =
                    (_storedOutputItem.transform.position - slotTransform.position).sqrMagnitude;

                if (sqrDistance > StoredItemSlotTolerance * StoredItemSlotTolerance)
                {
                    _storedOutputItem = null;
                    return null;
                }
            }

            return _storedOutputItem;
        }

        private void TryTakeCompletedItem(PlayerInteractionAbility interactionAbility)
        {
            ItemObject storedOutputItem = GetStoredOutputItem();
            if (storedOutputItem == null)
                return;

            if (!storedOutputItem.TryGetComponent(out IInteractable interactable))
                return;

            if (interactionAbility.TryStartHoldFromExternal(interactable))
            {
                _storedOutputItem = null;
            }
        }

        private void TryInsertTray(
            PlayerInteractionAbility interactionAbility,
            TrayItem trayItem)
        {
            if (_loadedTrayItems.Count >= MaxTraySlots)
                return;

            if (!_sterilizationMachine.CanSterilizeTray(trayItem))
                return;

            if (!interactionAbility.TryReleaseHeldItem(trayItem, returnOwnershipToMaster: false))
                return;

            PlaceStoredTray(trayItem, _loadedTrayItems.Count);
            SetStoredItemInteractionEnabled(trayItem, false);
            _loadedTrayItems.Add(trayItem);
        }

        private void TrySterilizeToolItem(
            PlayerInteractionAbility interactionAbility,
            MixToolItem mixToolItem)
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

            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.Destroy(mixToolItem.gameObject);
            }
            else
            {
                Destroy(mixToolItem.gameObject);
            }

            _processingToolResultMaterial = result.ResultMaterial;

            if (_traySterilizationTimer == null)
            {
                CompleteToolSterilization();
                return;
            }

            _traySterilizationTimer.TryStart(
                _toolSterilizationDuration,
                CompleteToolSterilization);
        }

        private Transform GetSlotTransform()
        {
            return transform;
        }

        private Transform GetTraySlotTransform(int slotIndex)
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

        private void PlaceStoredTray(TrayItem trayItem, int slotIndex)
        {
            if (trayItem == null)
                return;

            Transform slotTransform = GetTraySlotTransform(slotIndex);

            if (trayItem.TryGetComponent(out HoldableItem holdableItem))
            {
                holdableItem.Place(slotTransform);
            }
            else
            {
                trayItem.transform.SetPositionAndRotation(slotTransform.position, slotTransform.rotation);
            }

            PhotonView photonView = trayItem.GetComponent<PhotonView>();
            if (photonView != null && PhotonNetwork.MasterClient != null)
            {
                photonView.TransferOwnership(PhotonNetwork.MasterClient);
            }
        }

        private void PlaceStoredItem(ItemObject itemObject)
        {
            if (itemObject == null)
                return;

            Transform slotTransform = _toolSlotPoint != null ? _toolSlotPoint : GetSlotTransform();

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

        private void StartTraySterilizationBatch()
        {
            if (_loadedTrayItems.Count == 0 || _areLoadedTraysSterilized)
                return;

            if (_traySterilizationTimer == null)
            {
                CompleteTraySterilizationBatch();
                return;
            }

            _traySterilizationTimer.TryStart(
                _traySterilizationDuration,
                CompleteTraySterilizationBatch);
        }

        private void CompleteTraySterilizationBatch()
        {
            for (int i = 0; i < _loadedTrayItems.Count; i++)
            {
                TrayItem trayItem = _loadedTrayItems[i];
                if (trayItem == null)
                    continue;

                _sterilizationMachine.TrySterilizeTray(trayItem);
            }

            _areLoadedTraysSterilized = _loadedTrayItems.Count > 0;
        }

        private void CompleteToolSterilization()
        {
            if (_processingToolResultMaterial == CraftedMaterialType.Unknown)
                return;

            Transform slotTransform = _toolSlotPoint != null ? _toolSlotPoint : GetSlotTransform();
            GameObject resultObject = SpawnSterilizedResult(
                _processingToolResultMaterial,
                slotTransform.position,
                slotTransform.rotation);

            _processingToolResultMaterial = CraftedMaterialType.Unknown;

            if (resultObject == null)
                return;

            if (!resultObject.TryGetComponent(out ItemObject resultItem))
                return;

            PlaceStoredItem(resultItem);
            SetStoredItemInteractionEnabled(resultItem, false);
            _storedOutputItem = resultItem;
        }

        private void TryTakeCompletedTray(PlayerInteractionAbility interactionAbility)
        {
            TrayItem trayItem = GetNextCompletedTray();
            if (trayItem == null)
            {
                _areLoadedTraysSterilized = false;
                return;
            }

            if (!interactionAbility.TryStartHoldFromExternal(trayItem.GetComponent<IInteractable>()))
                return;

            _loadedTrayItems.Remove(trayItem);
            if (_loadedTrayItems.Count == 0)
                _areLoadedTraysSterilized = false;
        }

        private TrayItem GetNextCompletedTray()
        {
            for (int i = 0; i < _loadedTrayItems.Count; i++)
            {
                TrayItem trayItem = _loadedTrayItems[i];
                if (trayItem == null)
                    continue;

                return trayItem;
            }

            return null;
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
