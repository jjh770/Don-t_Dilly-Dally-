using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class TrayWorkbenchInteractable : MonoBehaviourPun, IInteractable, IItemAcceptor
    {
        [SerializeField] private TrayWorkbench _trayWorkbench;
        [SerializeField] private Transform _traySlotPoint;

        private int _stateRevision;

        public bool IsInteracting => false;
        public Transform Transform => transform;

        private void Awake()
        {
            if (_trayWorkbench == null)
            {
                _trayWorkbench = GetComponent<TrayWorkbench>();
            }

            if (_traySlotPoint == null)
            {
                _traySlotPoint = transform;
            }
        }

        private void LateUpdate()
        {
            ClearDetachedTrayState();

            TrayItem tray = _trayWorkbench != null ? _trayWorkbench.CurrentTrayItem : null;
            if (tray == null)
            {
                return;
            }

            tray.transform.localPosition = Vector3.zero;
            tray.transform.localRotation = Quaternion.identity;
        }

        public void Interact(Transform interactor)
        {
            if (_trayWorkbench == null || interactor == null)
            {
                return;
            }

            ClearDetachedTrayState();

            IHeldItemInteractor heldItemInteractor = interactor.GetComponent<IHeldItemInteractor>();
            if (heldItemInteractor == null)
            {
                return;
            }

            ItemObject heldItem = heldItemInteractor.CurrentHeldItem;
            if (heldItem == null)
            {
                TryTakeTray(heldItemInteractor);
                return;
            }

            if (heldItem is TrayItem trayItem)
            {
                TryPlaceTray(heldItemInteractor, trayItem);
                return;
            }

            if (!_trayWorkbench.HasTray)
            {
                return;
            }

            CraftedMaterialType materialType = ResolveMaterialType(heldItem);
            if (materialType != CraftedMaterialType.None)
            {
                TryPlaceMaterialOnTray(heldItemInteractor, heldItem, materialType);
            }
        }

        public void StopInteract()
        {
        }

        public bool CanAcceptItem(ItemObject item)
        {
            if (_trayWorkbench == null)
                return false;

            ClearDetachedTrayState();

            if (item is TrayItem trayItem)
                return _trayWorkbench.CanPlaceTrayItem(trayItem);

            if (!_trayWorkbench.HasTray)
                return false;

            CraftedMaterialType materialType = ResolveMaterialType(item);
            return materialType != CraftedMaterialType.None
                && _trayWorkbench.CanPlaceBasicMaterialOnTray(materialType);
        }

        #region Interaction Handlers

        private void TryPlaceTray(IHeldItemInteractor heldItemInteractor, TrayItem trayItem)
        {
            if (!_trayWorkbench.CanPlaceTrayItem(trayItem))
            {
                return;
            }

            if (!heldItemInteractor.TryReleaseHeldItem(trayItem))
            {
                return;
            }

            int trayViewId = trayItem.ViewId;

            _trayWorkbench.SetCurrentTrayItem(trayItem);
            TrayWorkbenchItemUtility.PlaceTrayOnWorkbench(trayItem, _traySlotPoint);
            int revision = NextStateRevision();

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_WorkbenchPlaceTray), RpcTarget.Others, trayViewId, revision);
            }
        }

        private void TryPlaceMaterialOnTray(IHeldItemInteractor heldItemInteractor, ItemObject itemObject, CraftedMaterialType materialType)
        {
            TrayItem trayItem = _trayWorkbench.CurrentTrayItem;
            if (trayItem == null)
            {
                return;
            }

            int playerId = PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : 0;

            CraftedItem craftedItem = CraftedItem.CreateBasicMaterial(materialType, playerId);
            if (craftedItem == null || !_trayWorkbench.CanPlaceItemOnTray(craftedItem))
            {
                return;
            }

            int availableSlotIndex = trayItem.GetFirstAvailableSlotIndex();
            if (availableSlotIndex < 0 || !trayItem.CanStoreItem(craftedItem, availableSlotIndex))
            {
                return;
            }

            if (!heldItemInteractor.TryReleaseHeldItem(itemObject))
            {
                return;
            }

            if (!trayItem.TryStoreItem(itemObject, craftedItem, availableSlotIndex))
            {
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_WorkbenchPlaceMaterial), RpcTarget.Others,
                    trayItem.ViewId, itemObject.ViewId, availableSlotIndex, (int)materialType, playerId);
            }
        }

        private static CraftedMaterialType ResolveMaterialType(ItemObject itemObject)
        {
            if (itemObject is BasicMaterialItem basicMaterialItem)
            {
                return basicMaterialItem.MaterialType;
            }

            if (itemObject is MixToolItem mixToolItem)
            {
                return mixToolItem.ToolType switch
                {
                    ToolType.PotionCyan    => CraftedMaterialType.FilledPotionCyan,
                    ToolType.PotionMagenta => CraftedMaterialType.FilledPotionMagenta,
                    ToolType.PotionYellow  => CraftedMaterialType.FilledPotionYellow,
                    _ => CraftedMaterialType.None
                };
            }

            return CraftedMaterialType.None;
        }

        private void ClearDetachedTrayState()
        {
            if (_trayWorkbench == null)
            {
                return;
            }

            TrayItem trayItem = _trayWorkbench.ResolvedCurrentTrayItem;
            if (trayItem == null)
            {
                return;
            }

            if (IsDetachedFromWorkbenchSlot(trayItem, _traySlotPoint))
            {
                _trayWorkbench.ClearCurrentTrayItem(trayItem);
            }
        }

        private static bool IsDetachedFromWorkbenchSlot(TrayItem trayItem, Transform expectedParent)
        {
            if (trayItem == null)
            {
                return false;
            }

            bool isInactive = !trayItem.gameObject.activeInHierarchy;
            bool parentMismatch = trayItem.transform.parent != expectedParent;
            bool isPendingRecycle = trayItem.IsPendingRecycle;
            bool isNoLongerStored = trayItem.TryGetComponent(out HoldableItem holdable) && !holdable.IsStoredInContainer;

            return isInactive || parentMismatch || isPendingRecycle || isNoLongerStored;
        }

        private void TryTakeTray(IHeldItemInteractor heldItemInteractor)
        {
            TrayItem trayItem = _trayWorkbench.CurrentTrayItem;
            if (trayItem == null)
            {
                return;
            }

            if (!trayItem.TryGetComponent(out HoldableItem holdable))
            {
                return;
            }

            heldItemInteractor.TryPickupInteractable(
                holdable,
                onBeforeHold: () =>
                {
                    if (_trayWorkbench.CurrentTrayItem != trayItem)
                    {
                        return false;
                    }

                    TrayWorkbenchItemUtility.PrepareTrayForPickup(trayItem);
                    _trayWorkbench.ClearCurrentTrayItem(trayItem);
                    int revision = NextStateRevision();

                    if (PhotonNetwork.InRoom)
                    {
                        photonView.RPC(nameof(RPC_WorkbenchTakeTray), RpcTarget.Others, trayItem.ViewId, revision);
                    }

                    return true;
                });
        }

        private void RollbackTakeTray(TrayItem trayItem)
        {
            if (trayItem == null)
                return;

            // 다른 플레이어가 이미 집었으면 롤백하지 않음
            if (trayItem.TryGetComponent(out HoldableItem holdable) && holdable.IsInteracting)
                return;

            _trayWorkbench.SetCurrentTrayItem(trayItem);
            TrayWorkbenchItemUtility.PlaceTrayOnWorkbench(trayItem, _traySlotPoint);

            if (PhotonNetwork.InRoom)
            {
                int viewId = trayItem.ViewId;
                photonView.RPC(nameof(RPC_WorkbenchRollbackTakeTray), RpcTarget.Others, viewId, NextStateRevision());
            }
        }

        #endregion

        #region RPC Handlers

        [PunRPC]
        private void RPC_WorkbenchRollbackTakeTray(int trayViewId, int revision)
        {
            if (!TryApplyStateRevision(revision))
            {
                return;
            }

            PhotonView trayPV = PhotonView.Find(trayViewId);
            if (trayPV == null || !trayPV.TryGetComponent(out TrayItem trayItem))
                return;

            // 다른 플레이어가 이미 집었으면 롤백하지 않음
            if (trayItem.TryGetComponent(out HoldableItem holdable) && holdable.IsInteracting)
                return;

            _trayWorkbench.SetCurrentTrayItem(trayItem);
            TrayWorkbenchItemUtility.PlaceTrayOnWorkbench(trayItem, _traySlotPoint);
        }

        [PunRPC]
        private void RPC_WorkbenchPlaceTray(int trayViewId, int revision)
        {
            if (!TryApplyStateRevision(revision))
            {
                return;
            }

            PhotonView trayPV = PhotonView.Find(trayViewId);
            if (trayPV == null || !trayPV.TryGetComponent(out TrayItem trayItem))
            {
                return;
            }

            _trayWorkbench.SetCurrentTrayItem(trayItem);
            TrayWorkbenchItemUtility.PlaceTrayOnWorkbench(trayItem, _traySlotPoint);
        }

        [PunRPC]
        private void RPC_WorkbenchPlaceMaterial(int trayViewId, int materialViewId, int slotIndex, int materialType, int playerId)
        {
            PhotonView trayPV = PhotonView.Find(trayViewId);
            if (trayPV == null || !trayPV.TryGetComponent(out TrayItem trayItem))
            {
                return;
            }

            PhotonView materialPV = PhotonView.Find(materialViewId);
            if (materialPV == null || !materialPV.TryGetComponent(out ItemObject itemObject))
            {
                return;
            }

            CraftedItem craftedItem = CraftedItem.CreateBasicMaterial((CraftedMaterialType)materialType, playerId);
            if (craftedItem == null)
            {
                return;
            }

            trayItem.TryStoreItem(itemObject, craftedItem, slotIndex);
        }

        [PunRPC]
        private void RPC_WorkbenchTakeTray(int trayViewId, int revision)
        {
            if (!TryApplyStateRevision(revision))
            {
                return;
            }

            TrayItem trayItem = null;
            PhotonView trayPV = PhotonView.Find(trayViewId);
            if (trayPV != null)
            {
                trayPV.TryGetComponent(out trayItem);
            }

            if (trayItem == null)
            {
                trayItem = _trayWorkbench.CurrentTrayItem;
            }

            if (trayItem != null)
            {
                TrayWorkbenchItemUtility.PrepareTrayForPickup(trayItem);
                if (_trayWorkbench.CurrentTrayItem == trayItem)
                {
                    _trayWorkbench.ClearCurrentTrayItem(trayItem);
                }
            }
        }

        private int NextStateRevision()
        {
            int nextRevision = PhotonNetwork.InRoom ? PhotonNetwork.ServerTimestamp : _stateRevision + 1;
            if (_stateRevision != 0 && (nextRevision - _stateRevision) <= 0)
            {
                nextRevision = _stateRevision + 1;
            }

            _stateRevision = nextRevision;
            return _stateRevision;
        }

        private bool TryApplyStateRevision(int revision)
        {
            if (_stateRevision != 0 && (revision - _stateRevision) < 0)
            {
                return false;
            }

            _stateRevision = revision;
            return true;
        }

        #endregion
    }
}
