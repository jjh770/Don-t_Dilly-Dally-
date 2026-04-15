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
            if (!PhotonNetwork.InRoom)
            {
                return false;
            }

            if (photonView == null)
            {
                Debug.LogWarning($"[ItemRecycle] Photon 룸 아이템 '{itemObject.name}'에 PhotonView가 없습니다.");
                return false;
            }

            if (!PhotonNetwork.IsMasterClient && !photonView.IsMine && !photonView.AmController)
            {
                return false;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                itemObject.RequestRecycleOnMaster();
                return true;
            }

            if (!itemObject.TryBeginRecycle())
            {
                return true;
            }

            RecycleAsMaster(itemObject);
            return true;
        }

        public static bool TryRecycleAsMaster(ItemObject itemObject)
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || itemObject == null)
            {
                return false;
            }

            if (!itemObject.TryBeginRecycle())
            {
                return true;
            }

            RecycleAsMaster(itemObject);
            return true;
        }

        private static void RecycleAsMaster(ItemObject itemObject)
        {
            itemObject.PrepareForRecycle();
            PhotonNetwork.Destroy(itemObject.gameObject);
        }
    }
}

