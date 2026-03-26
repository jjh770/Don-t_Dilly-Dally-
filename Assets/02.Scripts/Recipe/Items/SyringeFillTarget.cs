using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(MixToolItem))]
    public class SyringeFillTarget : MonoBehaviour, IInteractable
    {
        private const string ResultPrefabName = "BasicMaterialItem";
        private const float PendingOwnershipTimeout = 2f;
        private const ToolType FillInputMask = ToolType.Syringe | ToolType.AnestheticFluid | ToolType.SedativeFluid;

        [Header("주사기 주입 설정")]
        [SerializeField] private CraftingRuleDatabase _ruleDatabase;
        [SerializeField] private ActionTimer _actionTimer;
        [SerializeField] private GameObject _resultPrefab;

        private MixToolItem _mixToolItem;
        private NetworkItemOwnership _networkOwnership;
        private bool _isInteractionLocked;

        private PlayerInteractionAbility _pendingInteractionAbility;
        private ItemObject _pendingHeldItem;
        private FillResult _pendingFillResult;
        private float _pendingOwnershipElapsed;
        private bool _isAwaitingOwnership;
        private bool _hasOutstandingFillOwnershipRequest;

        private PlayerInteractionAbility _activeInteractionAbility;
        private ItemObject _activeHeldItem;
        private bool _isFillInProgress;
        private bool _isPlayerInteractionLocked;

        public bool IsInteracting => _isInteractionLocked;
        public Transform Transform => transform;

        private void Awake()
        {
            _mixToolItem = GetComponent<MixToolItem>();
            _networkOwnership = GetComponent<NetworkItemOwnership>();

            if (_actionTimer == null)
            {
                _actionTimer = GetComponentInChildren<ActionTimer>(true);
            }
        }

        private void OnEnable()
        {
            if (_networkOwnership != null)
            {
                _networkOwnership.OwnershipAcquiredLocally += HandleOwnershipAcquiredLocally;
            }
        }

        private void OnDisable()
        {
            if (_networkOwnership != null)
            {
                _networkOwnership.OwnershipAcquiredLocally -= HandleOwnershipAcquiredLocally;
            }

            AbortCurrentInteraction();
        }

        private void Update()
        {
            if (!_isAwaitingOwnership)
                return;

            _pendingOwnershipElapsed += Time.deltaTime;
            if (_pendingOwnershipElapsed >= PendingOwnershipTimeout)
            {
                ClearPendingState();
                ReleaseOwnershipToMasterIfNeeded();
            }
        }

        public bool CanInteractWith(ItemObject heldItem)
        {
            return ResolveFill(heldItem).Success;
        }

        public void Interact(Transform interactor)
        {
            if (_isInteractionLocked)
            {
                return;
            }

            if (interactor == null)
            {
                return;
            }

            PlayerInteractionAbility interactionAbility = interactor.GetComponent<PlayerInteractionAbility>();
            if (interactionAbility == null)
            {
                return;
            }

            ItemObject heldItem = interactionAbility.CurrentHeldItem;
            FillResult fillResult = ResolveFill(heldItem);
            if (!fillResult.Success)
            {
                return;
            }

            if (_networkOwnership != null && !_networkOwnership.IsOwnedLocally)
            {
                BeginPendingOwnershipRequest(interactionAbility, heldItem, fillResult);
                _networkOwnership.TryAcquireOrRequestOwnership();
                return;
            }

            StartFill(interactionAbility, heldItem, fillResult);
        }

        public void StopInteract()
        {
        }

        private void HandleOwnershipAcquiredLocally(NetworkItemOwnership ownership)
        {
            if (ownership != _networkOwnership)
            {
                return;
            }

            if (!_hasOutstandingFillOwnershipRequest)
            {
                return;
            }

            if (!_isAwaitingOwnership || _pendingInteractionAbility == null || _pendingHeldItem == null)
            {
                if (!_isFillInProgress)
                {
                    _hasOutstandingFillOwnershipRequest = false;
                    ReleaseOwnershipToMasterIfNeeded();
                }

                return;
            }

            _hasOutstandingFillOwnershipRequest = false;
            StartFill(_pendingInteractionAbility, _pendingHeldItem, _pendingFillResult);
        }

        private FillResult ResolveFill(ItemObject heldItem)
        {
            if (_ruleDatabase == null || _mixToolItem == null || heldItem is not MixToolItem heldMixToolItem)
            {
                return FillResult.Failure();
            }

            ToolType targetToolType = _mixToolItem.ToolType;
            ToolType heldToolType = heldMixToolItem.ToolType;

            if (!IsSupportedFillInput(targetToolType) || !IsSupportedFillInput(heldToolType))
            {
                return FillResult.Failure();
            }

            if (targetToolType == heldToolType)
            {
                return FillResult.Failure();
            }

            ToolType usedToolsMask = targetToolType | heldToolType;

            if ((usedToolsMask & ToolType.Syringe) == ToolType.None)
            {
                return FillResult.Failure();
            }

            ToolType fluidMask = usedToolsMask & (ToolType.AnestheticFluid | ToolType.SedativeFluid);
            if (fluidMask == ToolType.None || fluidMask == (ToolType.AnestheticFluid | ToolType.SedativeFluid))
            {
                return FillResult.Failure();
            }

            CraftingRuleSO rule = _ruleDatabase.FindRule(usedToolsMask, ActionType.Fill);
            if (rule == null || rule.ResultMaterial == CraftedMaterialType.Unknown)
            {
                return FillResult.Failure();
            }

            return FillResult.Succeed(usedToolsMask, rule.ResultMaterial, rule.CraftingDuration);
        }

        private void StartFill(PlayerInteractionAbility interactionAbility, ItemObject heldItem, FillResult fillResult)
        {
            if (_isInteractionLocked)
            {
                return;
            }

            if (interactionAbility == null || heldItem == null)
            {
                AbortCurrentInteraction();
                return;
            }

            ClearPendingState();

            if (!interactionAbility.TryBeginExternalInteractionLock(heldItem))
            {
                AbortCurrentInteraction();
                return;
            }

            _isInteractionLocked = true;
            _isFillInProgress = true;
            _isPlayerInteractionLocked = true;
            _activeInteractionAbility = interactionAbility;
            _activeHeldItem = heldItem;

            _networkOwnership?.LockOwnershipOnController();

            if (_actionTimer != null)
            {
                bool started = _actionTimer.TryStart(fillResult.FillDuration, () => CompleteFill(fillResult.ResultMaterial));
                if (started)
                {
                    return;
                }
            }

            CompleteFill(fillResult.ResultMaterial);
        }

        private void CompleteFill(CraftedMaterialType resultMaterial)
        {
            if (!_isFillInProgress)
            {
                return;
            }

            _isInteractionLocked = false;

            PlayerInteractionAbility interactionAbility = _activeInteractionAbility;
            ItemObject heldItem = _activeHeldItem;

            if (interactionAbility == null || heldItem == null)
            {
                FinishCurrentInteraction();
                return;
            }

            ReleaseInteractionLockIfNeeded();

            if (!interactionAbility.TryConsumeHeldItem(heldItem))
            {
                FinishCurrentInteraction();
                return;
            }

            Vector3 spawnPosition = transform.position;
            Quaternion spawnRotation = transform.rotation;

            GameObject spawnedObject = SpawnResult(resultMaterial, spawnPosition, spawnRotation);
            if (spawnedObject != null && spawnedObject.TryGetComponent(out IInteractable interactable))
            {
                interactionAbility.TryStartHoldFromExternal(interactable);
            }

            FinishCurrentInteraction();
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

        private void ClearPendingState()
        {
            _isAwaitingOwnership = false;
            _pendingInteractionAbility = null;
            _pendingHeldItem = null;
            _pendingFillResult = FillResult.Failure();
            _pendingOwnershipElapsed = 0f;
        }

        private void BeginPendingOwnershipRequest(
            PlayerInteractionAbility interactionAbility,
            ItemObject heldItem,
            FillResult fillResult)
        {
            _isAwaitingOwnership = true;
            _hasOutstandingFillOwnershipRequest = true;
            _pendingInteractionAbility = interactionAbility;
            _pendingHeldItem = heldItem;
            _pendingFillResult = fillResult;
            _pendingOwnershipElapsed = 0f;
        }

        private void FinishCurrentInteraction()
        {
            ReleaseInteractionLockIfNeeded();
            ClearPendingState();
            ClearActiveFillState();
            _hasOutstandingFillOwnershipRequest = false;
            _isInteractionLocked = false;
            _networkOwnership?.UnlockOwnershipOnController();
            ReleaseOwnershipToMasterIfNeeded();
        }

        private void AbortCurrentInteraction()
        {
            if (_isFillInProgress)
            {
                _actionTimer?.Cancel();
            }

            FinishCurrentInteraction();
        }

        private void ClearActiveFillState()
        {
            _activeInteractionAbility = null;
            _activeHeldItem = null;
            _isFillInProgress = false;
            _isPlayerInteractionLocked = false;
        }

        private void ReleaseInteractionLockIfNeeded()
        {
            if (!_isPlayerInteractionLocked)
            {
                return;
            }

            _activeInteractionAbility?.EndExternalInteractionLock();
            _isPlayerInteractionLocked = false;
        }

        private void ReleaseOwnershipToMasterIfNeeded()
        {
            if (!PhotonNetwork.InRoom || _networkOwnership == null || !_networkOwnership.IsOwnedLocally)
            {
                return;
            }

            if (PhotonNetwork.MasterClient == null)
            {
                return;
            }

            if (PhotonNetwork.LocalPlayer != null &&
                PhotonNetwork.MasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return;
            }

            PhotonView photonView = _networkOwnership.PhotonView;
            if (photonView == null)
            {
                return;
            }

            photonView.TransferOwnership(PhotonNetwork.MasterClient);
        }

        private static bool IsSupportedFillInput(ToolType toolType)
        {
            return toolType != ToolType.None && (toolType & FillInputMask) == toolType;
        }
    }
}
