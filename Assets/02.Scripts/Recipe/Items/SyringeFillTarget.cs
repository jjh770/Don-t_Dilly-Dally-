using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(MixToolItem))]
    public class SyringeFillTarget : MonoBehaviour, IInteractable
    {
        private const string ResultPrefabName = "BasicMaterialItem";
        private const ToolType FillInputMask = ToolType.Syringe | ToolType.AnestheticFluid | ToolType.SedativeFluid;

        [Header("주사기 주입 설정")]
        [SerializeField] private CraftingRuleDatabase _ruleDatabase;
        [SerializeField] private ActionTimer _actionTimer;
        [SerializeField] private GameObject _resultPrefab;
        private float _pendingFillDuration;

        private MixToolItem _mixToolItem;
        private Collider _targetCollider;
        private NetworkItemOwnership _networkOwnership;
        private bool _isInteractionLocked;

        private PlayerInteractionAbility _pendingInteractionAbility;
        private ItemObject _pendingHeldItem;
        private ToolType _pendingUsedToolsMask;
        private CraftedMaterialType _pendingResultMaterial;

        public bool IsInteracting => _isInteractionLocked;
        public Transform Transform => transform;

        private void Awake()
        {
            _mixToolItem = GetComponent<MixToolItem>();
            _targetCollider = GetComponent<Collider>();
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
        }

        public bool CanInteractWith(ItemObject heldItem)
        {
            return TryResolveFill(heldItem, out _, out _, out _);
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
            if (!TryResolveFill(heldItem, out ToolType usedToolsMask, out CraftedMaterialType resultMaterial, out float fillDuration))
            {
                return;
            }

            if (_networkOwnership != null && !_networkOwnership.IsOwnedLocally)
            {
                _pendingInteractionAbility = interactionAbility;
                _pendingHeldItem = heldItem;
                _pendingUsedToolsMask = usedToolsMask;
                _pendingResultMaterial = resultMaterial;
                _pendingFillDuration = fillDuration;
                _networkOwnership.TryAcquireOrRequestOwnership();
                return;
            }

            StartFill(interactionAbility, heldItem, resultMaterial, fillDuration);
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

            if (_pendingInteractionAbility == null || _pendingHeldItem == null)
            {
                return;
            }

            StartFill(_pendingInteractionAbility, _pendingHeldItem, _pendingResultMaterial, _pendingFillDuration);
        }

        private bool TryResolveFill(ItemObject heldItem, out ToolType usedToolsMask, out CraftedMaterialType resultMaterial, out float fillDuration)
        {
            usedToolsMask = ToolType.None;
            resultMaterial = CraftedMaterialType.Unknown;
            fillDuration = 0f;

            if (_ruleDatabase == null || _mixToolItem == null || heldItem is not MixToolItem heldMixToolItem)
            {
                return false;
            }

            ToolType targetToolType = _mixToolItem.ToolType;
            ToolType heldToolType = heldMixToolItem.ToolType;

            if (!IsSupportedFillInput(targetToolType) || !IsSupportedFillInput(heldToolType))
            {
                return false;
            }

            if (targetToolType == heldToolType)
            {
                return false;
            }

            usedToolsMask = targetToolType | heldToolType;

            if ((usedToolsMask & ToolType.Syringe) == ToolType.None)
            {
                return false;
            }

            ToolType fluidMask = usedToolsMask & (ToolType.AnestheticFluid | ToolType.SedativeFluid);
            if (fluidMask == ToolType.None || fluidMask == (ToolType.AnestheticFluid | ToolType.SedativeFluid))
            {
                return false;
            }

            CraftingRuleSO rule = _ruleDatabase.FindRule(usedToolsMask, ActionType.Fill);
            if (rule == null || rule.ResultMaterial == CraftedMaterialType.Unknown)
            {
                return false;
            }

            resultMaterial = rule.ResultMaterial;
            fillDuration = rule.CraftingDuration;
            return true;
        }

        private void StartFill(PlayerInteractionAbility interactionAbility, ItemObject heldItem, CraftedMaterialType resultMaterial, float fillDuration)
        {
            if (_isInteractionLocked)
            {
                return;
            }

            if (interactionAbility == null || heldItem == null)
            {
                ClearPendingState();
                return;
            }

            if (!interactionAbility.TryBeginExternalInteractionLock(heldItem))
            {
                ClearPendingState();
                return;
            }

            _isInteractionLocked = true;

            if (_actionTimer != null)
            {
                bool started = _actionTimer.TryStart(fillDuration, () => CompleteFill(interactionAbility, heldItem, resultMaterial));
                if (started)
                {
                    ClearPendingState();
                    return;
                }
            }

            CompleteFill(interactionAbility, heldItem, resultMaterial);
        }

        private void CompleteFill(PlayerInteractionAbility interactionAbility, ItemObject heldItem, CraftedMaterialType resultMaterial)
        {
            _isInteractionLocked = false;
            interactionAbility.EndExternalInteractionLock();

            if (!interactionAbility.TryConsumeHeldItem(heldItem))
            {
                ClearPendingState();
                return;
            }

            Vector3 spawnPosition = transform.position;
            Quaternion spawnRotation = transform.rotation;

            GameObject spawnedObject = SpawnResult(resultMaterial, spawnPosition, spawnRotation);
            if (spawnedObject != null && spawnedObject.TryGetComponent(out IInteractable interactable))
            {
                interactionAbility.TryStartHoldFromExternal(interactable);
            }

            ClearPendingState();
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
            _pendingInteractionAbility = null;
            _pendingHeldItem = null;
            _pendingUsedToolsMask = ToolType.None;
            _pendingResultMaterial = CraftedMaterialType.Unknown;
            _pendingFillDuration = 0f;
        }

        private static bool IsSupportedFillInput(ToolType toolType)
        {
            return toolType != ToolType.None && (toolType & FillInputMask) == toolType;
        }
    }
}
