using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class TrayWorkbenchInteractable : MonoBehaviourPun, IInteractable, IItemAcceptor
    {
        [SerializeField] private TrayWorkbench _trayWorkbench;
        [SerializeField] private Transform _traySlotPoint;

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

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_WorkbenchPlaceTray), RpcTarget.Others, trayViewId);
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
                    itemObject.ViewId, availableSlotIndex, (int)materialType, playerId);
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

            // 워크벤치 상태를 먼저 정리 (비마스터의 소유권 대기 중에도 즉시 반영)
            TrayWorkbenchItemUtility.PrepareTrayForPickup(trayItem);
            _trayWorkbench.ClearCurrentTrayItem(trayItem);

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_WorkbenchTakeTray), RpcTarget.Others);
            }

            TrayItem trayToRestore = trayItem;
            heldItemInteractor.TryPickupInteractable(holdable, () =>
            {
                RollbackTakeTray(trayToRestore);
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
                photonView.RPC(nameof(RPC_WorkbenchRollbackTakeTray), RpcTarget.Others, viewId);
            }
        }

        #endregion

        #region RPC Handlers

        [PunRPC]
        private void RPC_WorkbenchRollbackTakeTray(int trayViewId)
        {
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
        private void RPC_WorkbenchPlaceTray(int trayViewId)
        {
            PhotonView trayPV = PhotonView.Find(trayViewId);
            if (trayPV == null || !trayPV.TryGetComponent(out TrayItem trayItem))
            {
                return;
            }

            _trayWorkbench.SetCurrentTrayItem(trayItem);
            TrayWorkbenchItemUtility.PlaceTrayOnWorkbench(trayItem, _traySlotPoint);
        }

        [PunRPC]
        private void RPC_WorkbenchPlaceMaterial(int materialViewId, int slotIndex, int materialType, int playerId)
        {
            TrayItem trayItem = _trayWorkbench.CurrentTrayItem;
            if (trayItem == null)
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
        private void RPC_WorkbenchTakeTray()
        {
            TrayItem trayItem = _trayWorkbench.CurrentTrayItem;
            if (trayItem != null)
            {
                TrayWorkbenchItemUtility.PrepareTrayForPickup(trayItem);
                _trayWorkbench.ClearCurrentTrayItem(trayItem);
            }
        }

        #endregion
    }
}
