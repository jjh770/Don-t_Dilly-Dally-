using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class TrayWorkbenchInteractable : MonoBehaviour, IInteractable
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
            }
        }

        public void StopInteract()
        {
        }

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

            _trayWorkbench.SetCurrentTrayItem(trayItem);

            HoldableItem holdable = trayItem.GetComponent<HoldableItem>();
            if (holdable != null)
            {
                holdable.Place(_traySlotPoint);
            }
            else
            {
                trayItem.transform.SetPositionAndRotation(_traySlotPoint.position, _traySlotPoint.rotation);
            }

            PhotonView photonView = trayItem.GetComponent<PhotonView>();
            if (photonView != null && PhotonNetwork.MasterClient != null)
            {
                photonView.TransferOwnership(PhotonNetwork.MasterClient);
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

            trayItem.Slots.TryStoreItem(basicMaterialItem, availableSlotIndex);

            int playerId = PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : 0;

            _trayWorkbench.TryPlaceBasicMaterialOnTray(basicMaterialItem.MaterialType, playerId);
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

            if (!interactionAbility.TryStartHoldFromExternal(holdable))
            {
                return;
            }

            _trayWorkbench.ClearCurrentTrayItem(trayItem);
        }
    }
}
