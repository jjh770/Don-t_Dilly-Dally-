using Photon.Pun;
using System.Collections;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(MixToolItem), typeof(PhotonView))]
    public class SyringeFillTarget : MonoBehaviourPun, IInteractable, IItemAcceptor
    {
        private const string ResultPrefabName = "BasicMaterialItem";
        private const ToolType FillInputMask = ToolType.Syringe | ToolType.AnestheticFluid | ToolType.SedativeFluid;
        private const float ResultPickupTimeout = 2f;
        private const float ResultPickupPollInterval = 0.05f;

        [Header("주사기 주입 설정")]
        [SerializeField] private CraftingRuleDatabase _ruleDatabase;
        [SerializeField] private ActionTimer _actionTimer;

        private MixToolItem _mixToolItem;
        private NetworkItemOwnership _networkOwnership;
        private bool _isInteractionLocked;

        private IHeldItemInteractor _pendingHeldItemInteractor;
        private ItemObject _pendingHeldItem;
        private FillResult _pendingFillResult;

        private IHeldItemInteractor _activeHeldItemInteractor;
        private ItemObject _activeHeldItem;
        private IHeldItemInteractor _pendingResultPickupInteractor;
        private WaitForSeconds _resultPickupPollWait;
        private bool _isFillInProgress;
        private bool _isPlayerInteractionLocked;

        public bool IsInteracting => _isInteractionLocked;
        public Transform Transform => transform;

        private void Awake()
        {
            _mixToolItem = GetComponent<MixToolItem>();
            _networkOwnership = GetComponent<NetworkItemOwnership>();
            _resultPickupPollWait = new WaitForSeconds(ResultPickupPollInterval);

            if (_actionTimer == null)
            {
                _actionTimer = GetComponentInChildren<ActionTimer>(true);
            }
        }

        private void OnDisable()
        {
            _networkOwnership?.CancelPendingRequest();
            AbortCurrentInteraction(returnOwnershipToMaster: false);
        }

        public bool CanInteractWith(ItemObject heldItem)
        {
            return ResolveFill(heldItem).Success;
        }

        public bool CanAcceptItem(ItemObject item)
        {
            return CanInteractWith(item);
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

            IHeldItemInteractor heldItemInteractor = interactor.GetComponent<IHeldItemInteractor>();
            if (heldItemInteractor == null)
            {
                return;
            }

            ItemObject heldItem = heldItemInteractor.CurrentHeldItem;
            FillResult fillResult = ResolveFill(heldItem);
            if (!fillResult.Success)
            {
                return;
            }

            if (_networkOwnership != null && !_networkOwnership.IsOwnedLocally)
            {
                _pendingHeldItemInteractor = heldItemInteractor;
                _pendingHeldItem = heldItem;
                _pendingFillResult = fillResult;

                _networkOwnership.RequestOwnershipWithCallback(
                    onAcquired: () => StartFill(_pendingHeldItemInteractor, _pendingHeldItem, _pendingFillResult),
                    onFailed: () =>
                    {
                        ClearPendingState();
                        ReleaseOwnershipToMasterIfNeeded();
                    }
                );
                return;
            }

            StartFill(heldItemInteractor, heldItem, fillResult);
        }

        public void StopInteract()
        {
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

        private void StartFill(IHeldItemInteractor heldItemInteractor, ItemObject heldItem, FillResult fillResult)
        {
            if (_isInteractionLocked)
            {
                return;
            }

            if (heldItemInteractor == null || heldItem == null)
            {
                AbortCurrentInteraction();
                return;
            }

            ClearPendingState();

            if (!heldItemInteractor.TryBeginHeldItemInteractionLock(heldItem))
            {
                AbortCurrentInteraction();
                return;
            }

            _isInteractionLocked = true;
            _isFillInProgress = true;
            _isPlayerInteractionLocked = true;
            _activeHeldItemInteractor = heldItemInteractor;
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

            IHeldItemInteractor heldItemInteractor = _activeHeldItemInteractor;
            ItemObject heldItem = _activeHeldItem;

            if (heldItemInteractor == null || heldItem == null)
            {
                FinishCurrentInteraction();
                return;
            }

            ReleaseInteractionLockIfNeeded();

            if (!heldItemInteractor.TryConsumeHeldItem(heldItem))
            {
                FinishCurrentInteraction();
                return;
            }

            Vector3 spawnPosition = transform.position;
            Quaternion spawnRotation = transform.rotation;

            if (PhotonNetwork.IsMasterClient)
            {
                GameObject spawnedObject = SpawnResult(resultMaterial, spawnPosition, spawnRotation);
                if (spawnedObject != null && spawnedObject.TryGetComponent(out IInteractable interactable))
                {
                    heldItemInteractor.TryPickupInteractable(interactable);
                }

                FinishCurrentInteraction();
                return;
            }

            int requesterActorNumber = PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : -1;

            _pendingResultPickupInteractor = heldItemInteractor;
            FinishCurrentInteraction(clearPendingResultPickup: false);
            photonView.RPC(
                nameof(RPC_RequestSpawnFillResult),
                RpcTarget.MasterClient,
                (int)resultMaterial,
                spawnPosition,
                spawnRotation,
                requesterActorNumber);
        }

        private GameObject SpawnResult(CraftedMaterialType resultMaterial, Vector3 position, Quaternion rotation)
        {
            if (!PhotonNetwork.InRoom)
            {
                return null;
            }

            return PhotonNetwork.InstantiateRoomObject(ResultPrefabName, position, rotation, 0, new object[] { (int)resultMaterial });
        }

        [PunRPC]
        private void RPC_RequestSpawnFillResult(int resultMaterialValue, Vector3 position, Quaternion rotation, int requesterActorNumber)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            GameObject spawnedObject = SpawnResult((CraftedMaterialType)resultMaterialValue, position, rotation);
            if (spawnedObject == null || !spawnedObject.TryGetComponent(out PhotonView resultView))
            {
                return;
            }

            photonView.RPC(nameof(RPC_TryPickupSpawnedResult), RpcTarget.All, requesterActorNumber, resultView.ViewID);
        }

        [PunRPC]
        private void RPC_TryPickupSpawnedResult(int requesterActorNumber, int resultViewId)
        {
            if (PhotonNetwork.LocalPlayer == null ||
                PhotonNetwork.LocalPlayer.ActorNumber != requesterActorNumber)
            {
                return;
            }

            if (_pendingResultPickupInteractor == null)
            {
                return;
            }

            StartCoroutine(TryPickupSpawnedResultWhenAvailable(resultViewId));
        }

        private IEnumerator TryPickupSpawnedResultWhenAvailable(int resultViewId)
        {
            float elapsed = 0f;
            while (elapsed < ResultPickupTimeout)
            {
                PhotonView resultView = PhotonView.Find(resultViewId);
                if (resultView != null && resultView.TryGetComponent(out IInteractable interactable))
                {
                    _pendingResultPickupInteractor?.TryPickupInteractable(interactable);
                    _pendingResultPickupInteractor = null;
                    yield break;
                }

                elapsed += ResultPickupPollInterval;
                yield return _resultPickupPollWait;
            }

            _pendingResultPickupInteractor = null;
        }

        private void ClearPendingState()
        {
            _networkOwnership?.CancelPendingRequest();
            _pendingHeldItemInteractor = null;
            _pendingHeldItem = null;
            _pendingFillResult = FillResult.Failure();
        }

        private void FinishCurrentInteraction(bool returnOwnershipToMaster = true, bool clearPendingResultPickup = true)
        {
            ReleaseInteractionLockIfNeeded();
            ClearPendingState();
            ClearActiveFillState();
            _isInteractionLocked = false;
            _networkOwnership?.UnlockOwnershipOnController();
            if (clearPendingResultPickup)
            {
                _pendingResultPickupInteractor = null;
            }

            if (returnOwnershipToMaster)
            {
                ReleaseOwnershipToMasterIfNeeded();
            }
        }

        private void AbortCurrentInteraction(bool returnOwnershipToMaster = true)
        {
            if (_isFillInProgress)
            {
                _actionTimer?.Cancel();
            }

            FinishCurrentInteraction(returnOwnershipToMaster);
        }

        private void ClearActiveFillState()
        {
            _activeHeldItemInteractor = null;
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

            _activeHeldItemInteractor?.EndHeldItemInteractionLock();
            _isPlayerInteractionLocked = false;
        }

        private void ReleaseOwnershipToMasterIfNeeded()
        {
            if (_networkOwnership == null)
                return;

            NetworkItemOwnership.ReturnOwnershipToMaster(_networkOwnership.PhotonView);
        }

        private static bool IsSupportedFillInput(ToolType toolType)
        {
            return toolType != ToolType.None && (toolType & FillInputMask) == toolType;
        }
    }
}
