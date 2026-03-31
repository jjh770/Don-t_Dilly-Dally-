using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    public enum TrayKind
    {
        Normal = 0,
        Sterilized = 1
    }

    // 트레이의 도메인 상태만 관리하는 월드 오브젝트 컴포넌트입니다.
    [RequireComponent(typeof(TrayItemSlots))]
    public class TrayItem : ItemObject, IPunInstantiateMagicCallback
    {
        [Header("트레이 상태")]
        [Tooltip("이 트레이가 들고 있는 실제 제출 데이터입니다.")]
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
            ClearContentsAndSync();
            ResetTrayDataAndSync(false);
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
            int[] undestroyedViewIds = Slots?.ClearStoredItems();
            ResetTrayDataAndSync(isSterilized);

            // 로컬에서 파괴할 수 없는 아이템(소유권 없음)을 마스터에게 파괴 요청
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
        private void RPC_RequestDestroyItems(int[] viewIds)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            foreach (int viewId in viewIds)
            {
                PhotonView targetView = PhotonView.Find(viewId);
                if (targetView != null && (targetView.IsMine || targetView.AmController))
                {
                    PhotonNetwork.Destroy(targetView.gameObject);
                }
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
