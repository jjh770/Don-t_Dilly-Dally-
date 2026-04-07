using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    public enum TrayKind
    {
        Normal = 0,
        Sterilized = 1
    }

    // 트레이의 종류와 제출 데이터를 관리하는 월드 아이템 컴포넌트입니다.
    [RequireComponent(typeof(TrayItemSlots))]
    public class TrayItem : ItemObject, IPunInstantiateMagicCallback
    {
        [Header("트레이 상태")]
        [Tooltip("현재 트레이가 담고 있는 실제 제출 데이터입니다.")]
        public SubmittedTray TrayData = new SubmittedTray();

        public bool HasTrayData => TrayData != null;
        public TrayKind Kind => TrayData != null ? TrayData.Kind : TrayKind.Normal;
        public bool IsSterilizedTray => Kind == TrayKind.Sterilized;
        public TrayItemSlots Slots { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Slots = GetComponent<TrayItemSlots>();
        }

        public override void Initialize(string displayName, GameObject modelPrefab = null)
        {
            InitializeTray(displayName, modelPrefab, isSterilized: false);
        }

        public override void PrepareForRecycle()
        {
            base.PrepareForRecycle();

            bool hasStoredItems = Slots != null && Slots.HasStoredItems();
            bool hasTrayItems = TrayData != null && TrayData.HasAnyItems();
            if (!hasStoredItems && !hasTrayItems)
            {
                return;
            }

            ClearContentsAndSync();
        }

        public void InitializeTray(string displayName, GameObject modelPrefab = null, bool isSterilized = false)
        {
            base.Initialize(displayName, modelPrefab);
            ResetTrayData(isSterilized);
        }

        public void EnsureTrayData()
        {
            if (TrayData == null)
            {
                TrayData = new SubmittedTray();
            }
        }

        public void ResetTrayData(bool isSterilized = false)
        {
            TrayData = new SubmittedTray
            {
                Kind = isSterilized ? TrayKind.Sterilized : TrayKind.Normal
            };
        }

        public void ResetTrayDataAndSync(bool isSterilized = false)
        {
            ResetTrayData(isSterilized);

            PhotonView photonView = GetComponent<PhotonView>();
            if (photonView == null || !PhotonNetwork.InRoom || !photonView.IsMine)
            {
                return;
            }

            photonView.RPC(nameof(RPC_ResetTrayData), RpcTarget.Others, isSterilized);
        }

        public void ClearContentsAndSync()
        {
            bool isSterilized = IsSterilizedTray;

            // 트레이 Destroy 이벤트보다 먼저 도착하도록
            // 다른 클라이언트에서 자식 아이템을 분리합니다.
            int[] storedViewIds = Slots?.GetStoredItemViewIds();
            if (storedViewIds != null && storedViewIds.Length > 0 && PhotonNetwork.InRoom)
            {
                PhotonView pv = GetComponent<PhotonView>();
                if (pv != null)
                {
                    pv.RPC(nameof(RPC_DetachStoredItems), RpcTarget.Others, storedViewIds);
                }
            }

            int[] undestroyedViewIds = Slots?.ClearStoredItems();
            ResetTrayDataAndSync(isSterilized);

            // 로컬에서 직접 회수하지 못한 아이템은 마스터에게 정리를 요청합니다.
            if (undestroyedViewIds != null && undestroyedViewIds.Length > 0 && PhotonNetwork.InRoom)
            {
                PhotonView pv = GetComponent<PhotonView>();
                if (pv != null)
                {
                    pv.RPC(nameof(RPC_RequestDestroyItems), RpcTarget.MasterClient, undestroyedViewIds);
                }
            }
        }

        [PunRPC]
        private void RPC_DetachStoredItems(int[] viewIds)
        {
            if (viewIds == null)
            {
                return;
            }

            foreach (int viewId in viewIds)
            {
                PhotonView itemPV = PhotonView.Find(viewId);
                if (itemPV != null)
                {
                    itemPV.transform.SetParent(null, true);
                }
            }
        }

        [PunRPC]
        private void RPC_RequestDestroyItems(int[] viewIds)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            foreach (int viewId in viewIds)
            {
                PhotonView targetView = PhotonView.Find(viewId);
                if (targetView == null)
                {
                    continue;
                }

                if (!targetView.TryGetComponent(out ItemObject targetItem))
                {
                    continue;
                }

                ItemRecycleUtility.TryRecycle(targetItem);
            }
        }

        public void LoadTrayData(SubmittedTray trayData)
        {
            TrayData = trayData ?? new SubmittedTray();
        }

        public void SetTrayKind(TrayKind trayKind)
        {
            EnsureTrayData();
            TrayData.SetTrayKind(trayKind);
        }

        public void SetTrayKindAndSync(TrayKind trayKind)
        {
            SetTrayKind(trayKind);

            PhotonView photonView = GetComponent<PhotonView>();
            if (photonView == null || !PhotonNetwork.InRoom || !photonView.IsMine)
            {
                return;
            }

            photonView.RPC(nameof(RPC_SetTrayKind), RpcTarget.Others, (int)trayKind);
        }

        public void MarkSterilized()
        {
            SetTrayKind(TrayKind.Sterilized);
        }

        public void MarkContaminated()
        {
            SetTrayKind(TrayKind.Normal);
        }

        [PunRPC]
        private void RPC_SetTrayKind(int trayKindValue)
        {
            if (!System.Enum.IsDefined(typeof(TrayKind), trayKindValue))
            {
                return;
            }

            SetTrayKind((TrayKind)trayKindValue);
        }

        [PunRPC]
        private void RPC_ResetTrayData(bool isSterilized)
        {
            ResetTrayData(isSterilized);
        }

        public bool TryAddItem(CraftedItem item)
        {
            EnsureTrayData();
            return TrayData.TryAddItem(item);
        }

        public CraftedItem TakeLastItem()
        {
            EnsureTrayData();
            return TrayData.TakeLastItem();
        }

        public SubmittedTray GetTraySnapshot()
        {
            EnsureTrayData();
            return TrayData.Clone();
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            ResetSourceState();
            SetAsSupplyItem();
            ResetTrayData(false);
        }
    }
}
