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

            if (!PhotonNetwork.IsMasterClient && !photonView.IsMine)
            {
                return false;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                itemObject.RequestRecycleOnMaster();
                return true;
            }

            return TryRecycleAsMaster(itemObject);
        }

        public static bool TryRecycleAsMaster(ItemObject itemObject)
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || itemObject == null)
            {
                return false;
            }

            PhotonView photonView = itemObject.PhotonView;
            if (photonView == null)
            {
                return false;
            }

            if (!itemObject.TryBeginRecycle())
            {
                return true;
            }

            if (photonView.IsMine)
            {
                RecycleNetworkedObject(itemObject);
                return true;
            }

            NetworkItemOwnership networkOwnership = itemObject.NetworkOwnership;
            if (networkOwnership == null)
            {
                itemObject.ResetRecycleState();
                return false;
            }

            networkOwnership.RequestOwnershipWithCallback(
                onAcquired: () => RecycleAfterMasterOwnershipAcquired(itemObject),
                onFailed: itemObject.ResetRecycleState);

            return true;
        }

        private static void RecycleAfterMasterOwnershipAcquired(ItemObject itemObject)
        {
            if (itemObject == null)
            {
                return;
            }

            PhotonView photonView = itemObject.PhotonView;
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || photonView == null || !photonView.IsMine)
            {
                itemObject.ResetRecycleState();
                return;
            }

            RecycleNetworkedObject(itemObject);
        }

        private static void RecycleNetworkedObject(ItemObject itemObject)
        {
            itemObject.PrepareForRecycle();
            PhotonNetwork.Destroy(itemObject.gameObject);
        }
    }
}

