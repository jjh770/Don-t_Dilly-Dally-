using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class TrayWorkbenchInteractable : MonoBehaviourPun, IInteractable
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

            PlayerInteractionAbility interactionAbility = interactor.GetComponent<PlayerInteractionAbility>();
            if (interactionAbility == null)
            {
                return;
            }

            ItemObject heldItem = interactionAbility.CurrentHeldItem;
            if (heldItem == null)
            {
                TryTakeTray(interactionAbility);
                return;
            }

            if (heldItem is TrayItem trayItem)
            {
                TryPlaceTray(interactionAbility, trayItem);
                return;
            }

            if (!_trayWorkbench.HasTray)
            {
                return;
            }

            if (heldItem is BasicMaterialItem basicMaterialItem)
            {
                TryPlaceBasicMaterial(interactionAbility, basicMaterialItem);
                return;
            }

            if (heldItem is MixToolItem mixToolItem)
            {
                TryPlaceMixToolItem(interactionAbility, mixToolItem);
            }
        }

        public void StopInteract()
        {
        }

        #region Interaction Handlers

        private void TryPlaceTray(PlayerInteractionAbility interactionAbility, TrayItem trayItem)
        {
            if (!_trayWorkbench.CanPlaceTrayItem(trayItem))
            {
                return;
            }

            if (!interactionAbility.TryReleaseHeldItem(trayItem, returnOwnershipToMaster: false))
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

        private void TryPlaceBasicMaterial(PlayerInteractionAbility interactionAbility, BasicMaterialItem basicMaterialItem)
        {
            TrayItem trayItem = _trayWorkbench.CurrentTrayItem;
            if (trayItem == null || trayItem.Slots == null)
            {
                return;
            }

            if (!_trayWorkbench.CanPlaceBasicMaterialOnTray(basicMaterialItem.MaterialType))
            {
                return;
            }

            int availableSlotIndex = trayItem.Slots.GetFirstAvailableSlotIndex();
            if (availableSlotIndex < 0)
            {
                return;
            }

            if (!interactionAbility.TryReleaseHeldItem(basicMaterialItem, returnOwnershipToMaster: false))
            {
                return;
            }

            int materialViewId = GetPhotonViewId(basicMaterialItem);

            trayItem.Slots.TryStoreItem(basicMaterialItem, availableSlotIndex);

            int playerId = PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : 0;

            _trayWorkbench.TryPlaceBasicMaterialOnTray(basicMaterialItem.MaterialType, playerId);

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_WorkbenchPlaceMaterial), RpcTarget.Others,
                    materialViewId, availableSlotIndex, (int)basicMaterialItem.MaterialType, playerId);
            }
        }

        private void TryPlaceMixToolItem(PlayerInteractionAbility interactionAbility, MixToolItem mixToolItem)
        {
            CraftedMaterialType materialType = ResolveMixToolMaterialType(mixToolItem.ToolType);
            if (materialType == CraftedMaterialType.None)
            {
                return;
            }

            TrayItem trayItem = _trayWorkbench.CurrentTrayItem;
            if (trayItem == null || trayItem.Slots == null)
            {
                return;
            }

            if (!_trayWorkbench.CanPlaceBasicMaterialOnTray(materialType))
            {
                return;
            }

            int availableSlotIndex = trayItem.Slots.GetFirstAvailableSlotIndex();
            if (availableSlotIndex < 0)
            {
                return;
            }

            if (!interactionAbility.TryReleaseHeldItem(mixToolItem, returnOwnershipToMaster: false))
            {
                return;
            }

            int itemViewId = GetPhotonViewId(mixToolItem);

            trayItem.Slots.TryStoreItem(mixToolItem, availableSlotIndex);

            int playerId = PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : 0;

            _trayWorkbench.TryPlaceBasicMaterialOnTray(materialType, playerId);

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

        private void TryTakeTray(PlayerInteractionAbility interactionAbility)
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

            // 반환값 무시: 비마스터는 false를 반환하지만 pending hold로 자동 처리됨
            interactionAbility.TryStartHoldFromExternal(holdable);
        }

        #endregion

        #region RPC Handlers

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
            if (trayItem == null || trayItem.Slots == null)
            {
                return;
            }

            PhotonView materialPV = PhotonView.Find(materialViewId);
            if (materialPV == null || !materialPV.TryGetComponent(out ItemObject itemObject))
            {
                return;
            }

            trayItem.Slots.TryStoreItem(itemObject, slotIndex);
            _trayWorkbench.TryPlaceBasicMaterialOnTray((CraftedMaterialType)materialType, playerId);
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
            if (holdable != null)
            {
                holdable.Place(_traySlotPoint);

                // 워크벤치에 적재된 트레이는 콜라이더 비활성화
                // (플레이어는 워크벤치 콜라이더로 상호작용하므로 트레이 콜라이더 불필요)
                // 네트워크 동기화가 콜라이더를 다시 켜는 것도 방지
                holdable.SetStoredInContainer(true);
            }
            else
            {
                trayItem.transform.SetPositionAndRotation(_traySlotPoint.position, _traySlotPoint.rotation);
            }

            trayItem.transform.SetParent(_traySlotPoint, true);
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

            PhotonView pv = itemObject.GetComponent<PhotonView>();
            return pv != null ? pv.ViewID : -1;
        }
        #endregion
    }
}
