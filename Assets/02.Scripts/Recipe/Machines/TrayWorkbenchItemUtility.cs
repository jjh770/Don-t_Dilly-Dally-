using UnityEngine;

namespace DontDillyDally.Data
{
    internal static class TrayWorkbenchItemUtility
    {
        public static void PlaceTrayOnWorkbench(TrayItem trayItem, Transform slotTransform)
        {
            if (trayItem == null || slotTransform == null)
            {
                return;
            }

            bool isLocalOwner = trayItem.PhotonView != null && trayItem.PhotonView.IsMine;
            if (trayItem.TryGetComponent(out HoldableItem holdable))
            {
                if (isLocalOwner)
                {
                    holdable.Place(slotTransform);
                }
                else
                {
                    holdable.ApplyNetworkHoldState(false, -1);
                }

                holdable.SetStoredInContainer(true);
            }

            // Parent changes are not synchronized by PhotonTransformView, so every client pins the tray to the same slot.
            trayItem.transform.SetParent(slotTransform, false);
            trayItem.transform.localPosition = Vector3.zero;
            trayItem.transform.localRotation = Quaternion.identity;

            NetworkItemOwnership.ReturnOwnershipToMaster(trayItem.PhotonView);
        }

        public static void PrepareTrayForPickup(TrayItem trayItem)
        {
            if (trayItem == null)
            {
                return;
            }

            if (trayItem.TryGetComponent(out HoldableItem holdable))
            {
                holdable.SetStoredInContainer(false);
            }

            trayItem.transform.SetParent(null, true);
        }
    }
}
