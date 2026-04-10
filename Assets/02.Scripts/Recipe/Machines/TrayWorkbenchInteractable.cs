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

            if (heldItem is BasicMaterialItem basicMaterialItem)
            {
                TryPlaceBasicMaterial(heldItemInteractor, basicMaterialItem);
                return;
            }

            if (heldItem is MixToolItem mixToolItem)
            {
                TryPlaceMixToolItem(heldItemInteractor, mixToolItem);
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

            if (item is BasicMaterialItem basicMaterialItem)
                return _trayWorkbench.CanPlaceBasicMaterialOnTray(basicMaterialItem.MaterialType);

            if (item is MixToolItem mixToolItem)
            {
                CraftedMaterialType materialType = ResolveMixToolMaterialType(mixToolItem.ToolType);
                return materialType != CraftedMaterialType.None
                    && _trayWorkbench.CanPlaceBasicMaterialOnTray(materialType);
            }

            return false;
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

            int trayViewId = GetPhotonViewId(trayItem);

            _trayWorkbench.SetCurrentTrayItem(trayItem);
            PlaceTrayAtSlot(trayItem);

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_WorkbenchPlaceTray), RpcTarget.Others, trayViewId);
            }
        }

        private void TryPlaceBasicMaterial(IHeldItemInteractor heldItemInteractor, BasicMaterialItem basicMaterialItem)
        {
            TrayItem trayItem = _trayWorkbench.CurrentTrayItem;
            if (trayItem == null)
            {
                return;
            }

            int playerId = PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : 0;

            CraftedItem craftedItem = CraftedItem.CreateBasicMaterial(basicMaterialItem.MaterialType, playerId);
            if (craftedItem == null || !_trayWorkbench.CanPlaceItemOnTray(craftedItem))
            {
                return;
            }

            int availableSlotIndex = trayItem.GetFirstAvailableSlotIndex();
            if (availableSlotIndex < 0 || !trayItem.CanStoreItem(craftedItem, availableSlotIndex))
            {
                return;
            }

            if (!heldItemInteractor.TryReleaseHeldItem(basicMaterialItem))
            {
                return;
            }

            int materialViewId = GetPhotonViewId(basicMaterialItem);
            if (!trayItem.TryStoreItem(basicMaterialItem, craftedItem, availableSlotIndex))
            {
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_WorkbenchPlaceMaterial), RpcTarget.Others,
                    materialViewId, availableSlotIndex, (int)basicMaterialItem.MaterialType, playerId);
            }
        }

        private void TryPlaceMixToolItem(IHeldItemInteractor heldItemInteractor, MixToolItem mixToolItem)
        {
            CraftedMaterialType materialType = ResolveMixToolMaterialType(mixToolItem.ToolType);
            if (materialType == CraftedMaterialType.None)
            {
                return;
            }

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

            if (!heldItemInteractor.TryReleaseHeldItem(mixToolItem))
            {
                return;
            }

            int itemViewId = GetPhotonViewId(mixToolItem);
            if (!trayItem.TryStoreItem(mixToolItem, craftedItem, availableSlotIndex))
            {
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_WorkbenchPlaceMaterial), RpcTarget.Others,
                    itemViewId, availableSlotIndex, (int)materialType, playerId);
            }
        }

        private static CraftedMaterialType ResolveMixToolMaterialType(ToolType toolType)
        {
            return toolType switch
            {
                ToolType.PotionCyan    => CraftedMaterialType.FilledPotionCyan,
                ToolType.PotionMagenta => CraftedMaterialType.FilledPotionMagenta,
                ToolType.PotionYellow  => CraftedMaterialType.FilledPotionYellow,
                _ => CraftedMaterialType.None
            };
        }

        private void TryTakeTray(IHeldItemInteractor heldItemInteractor)
        {
            TrayItem trayItem = _trayWorkbench.CurrentTrayItem;
            if (trayItem == null)
            {
                return;
            }

            HoldableItem holdable = trayItem.GetComponent<HoldableItem>();
            if (holdable == null)
            {
                return;
            }

            // 워크벤치 상태를 먼저 정리 (비마스터의 소유권 대기 중에도 즉시 반영)
            SetTrayInteractionEnabled(trayItem, true);
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

            // 트레이 픽업 실패 시에는 손에 들린 상태가 아니라 작업대에 놓인 상태로 다시 고정합니다.
            _trayWorkbench.SetCurrentTrayItem(trayItem);
            PlaceTrayAtSlot(trayItem);
            SetTrayInteractionEnabled(trayItem, false);

            if (PhotonNetwork.InRoom)
            {
                int viewId = GetPhotonViewId(trayItem);
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

            _trayWorkbench.SetCurrentTrayItem(trayItem);
            PlaceTrayAtSlot(trayItem);
            SetTrayInteractionEnabled(trayItem, false);
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
            PlaceTrayAtSlot(trayItem);
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
                SetTrayInteractionEnabled(trayItem, true);
                _trayWorkbench.ClearCurrentTrayItem(trayItem);
            }
        }

        #endregion

        #region Utility

        private void PlaceTrayAtSlot(TrayItem trayItem)
        {
            HoldableItem holdable = trayItem.GetComponent<HoldableItem>();
            bool isLocalOwner = trayItem.PhotonView != null && trayItem.PhotonView.IsMine;

            if (holdable != null)
            {
                if (isLocalOwner)
                {
                    holdable.Place(_traySlotPoint);
                }
                else
                {
                    holdable.ApplyNetworkHoldState(false, -1);
                }

                holdable.SetStoredInContainer(true);
            }

            // 부모 변경 자체는 PhotonTransformView가 동기화하지 않습니다.
            // 배치 직후 소유권이 바로 바뀌더라도 모든 클라이언트가
            // 동일한 슬롯 기준 좌표를 사용하도록 로컬 좌표를 고정합니다.
            trayItem.transform.SetParent(_traySlotPoint, false);
            trayItem.transform.localPosition = Vector3.zero;
            trayItem.transform.localRotation = Quaternion.identity;

            NetworkItemOwnership.ReturnOwnershipToMaster(trayItem.PhotonView);
        }

        private static void SetTrayInteractionEnabled(TrayItem trayItem, bool isEnabled)
        {
            if (trayItem == null)
            {
                return;
            }

            Collider[] colliders = trayItem.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                ItemObject ownerItem = col.GetComponentInParent<ItemObject>();
                if (ownerItem != null && ownerItem != trayItem)
                {
                    continue;
                }

                col.enabled = isEnabled;
            }

            HoldableItem holdable = trayItem.GetComponent<HoldableItem>();
            if (holdable != null)
            {
                holdable.SetStoredInContainer(!isEnabled);
            }

            if (isEnabled)
            {
                trayItem.transform.SetParent(null, true);
            }
        }

        private static int GetPhotonViewId(ItemObject itemObject)
        {
            if (itemObject == null)
            {
                return -1;
            }

            return itemObject.ViewId;
        }
        #endregion
    }
}
