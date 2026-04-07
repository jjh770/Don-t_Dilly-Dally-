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

            if (itemObject.IsPendingRecycle)
            {
                return true;
            }

            PhotonView photonView = itemObject.PhotonView;
            if (PhotonNetwork.InRoom)
            {
                if (photonView == null)
                {
                    Debug.LogWarning($"[ItemRecycle] Photon 룸 아이템 '{itemObject.name}'에 PhotonView가 없습니다.");
                    return false;
                }

                if (!photonView.IsMine && !photonView.AmController)
                {
                    return false;
                }
            }

            if (!itemObject.TryBeginRecycle())
            {
                return true;
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
            itemObject.PrepareForRecycle();
        }
    }
}

