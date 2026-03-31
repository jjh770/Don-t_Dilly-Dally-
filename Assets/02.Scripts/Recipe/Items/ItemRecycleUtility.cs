using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    public static class ItemRecycleUtility
    {
        public static bool TryRecycle(ItemObject itemObject)
        {
            if (itemObject == null)
            {
                return false;
            }

            PhotonView photonView = itemObject.GetComponent<PhotonView>();
            if (PhotonNetwork.InRoom)
            {
                if (photonView == null)
                {
                    return false;
                }

                if (!photonView.IsMine && !photonView.AmController)
                {
                    return false;
                }
            }

            PrepareForRecycle(itemObject);

            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.Destroy(itemObject.gameObject);
                return true;
            }

            if (PunPoolManager.Instance != null && itemObject.TryGetComponent(out PoolableObject _))
            {
                PunPoolManager.Instance.Destroy(itemObject.gameObject);
                return true;
            }

            Object.Destroy(itemObject.gameObject);
            return true;
        }

        private static void PrepareForRecycle(ItemObject itemObject)
        {
            itemObject.transform.SetParent(null, true);

            if (itemObject.TryGetComponent(out HoldableItem holdableItem))
            {
                holdableItem.StopInteract();
                holdableItem.SetStoredInContainer(false);
            }

            if (itemObject is TrayItem trayItem)
            {
                trayItem.ClearContentsAndSync();
                trayItem.ResetTrayDataAndSync(false);
            }
        }
    }
}
