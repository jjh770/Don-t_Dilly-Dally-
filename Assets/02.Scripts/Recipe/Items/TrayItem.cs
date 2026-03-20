using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    public enum TrayKind
    {
        Normal = 0,
        Sterilized = 1
    }

    // 트레이 월드 오브젝트가 보관하는 상태 컴포넌트입니다.
    // 트레이 내부 데이터와 트레이 종류를 함께 관리합니다.
    public class TrayItem : ItemObject, IPunInstantiateMagicCallback
    {
        [Header("트레이 상태")]
        [Tooltip("이 트레이가 담고 있는 실제 제출 데이터")]
        public SubmittedTray TrayData = new SubmittedTray();

        [SerializeField] private TrayKind _trayKind = TrayKind.Normal;

        public bool HasTrayData => TrayData != null;
        public TrayKind Kind => _trayKind;
        public bool IsSterilizedTray => _trayKind == TrayKind.Sterilized;

        public override void Initialize(string displayName, GameObject modelPrefab = null)
        {
            InitializeTray(displayName, modelPrefab, isSterilized: false);
        }

        public void InitializeTray(
            string displayName,
            GameObject modelPrefab = null,
            bool isSterilized = false)
        {
            base.Initialize(displayName, modelPrefab);
            ResetTrayData(isSterilized);
        }

        public void EnsureTrayData()
        {
            if (TrayData == null)
                TrayData = new SubmittedTray();
        }

        public void ResetTrayData(bool isSterilized = false)
        {
            TrayData = new SubmittedTray
            {
                IsSterilized = isSterilized
            };

            _trayKind = isSterilized ? TrayKind.Sterilized : TrayKind.Normal;
        }

        public void LoadTrayData(SubmittedTray trayData)
        {
            TrayData = trayData ?? new SubmittedTray();
            _trayKind = TrayData.IsSterilized ? TrayKind.Sterilized : TrayKind.Normal;
        }

        public void SetTrayKind(TrayKind trayKind)
        {
            _trayKind = trayKind;
            EnsureTrayData();
            TrayData.IsSterilized = trayKind == TrayKind.Sterilized;
        }

        public void SetTrayKindAndSync(TrayKind trayKind)
        {
            SetTrayKind(trayKind);

            PhotonView photonView = GetComponent<PhotonView>();
            if (photonView == null || !PhotonNetwork.InRoom || !photonView.IsMine)
                return;

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
                return;

            SetTrayKind((TrayKind)trayKindValue);
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

        public void ClearItems()
        {
            EnsureTrayData();
            TrayData.ClearItems();
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
