using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("TrayData")]
        [SerializeField] private SubmittedTray _trayData = new SubmittedTray();
        [SerializeField] private GameObject _normalVisual;
        [SerializeField] private GameObject _sterilizedVisual;

        public TrayKind Kind => _trayData != null ? _trayData.Kind : TrayKind.Normal;
        public bool IsSterilizedTray => Kind == TrayKind.Sterilized;
        private TrayItemSlots _slots;

        protected override void Awake()
        {
            base.Awake();
            _slots = GetComponent<TrayItemSlots>();
            RefreshTrayVisual();
        }

        public override void Initialize(string displayName, GameObject modelPrefab = null)
        {
            InitializeTray(displayName, modelPrefab, isSterilized: false);
        }

        public override void PrepareForRecycle()
        {
            base.PrepareForRecycle();

            bool hasStoredItems = _slots != null && _slots.HasStoredItems();
            bool hasTrayItems = _trayData != null && _trayData.HasAnyItems();
            if (!hasStoredItems && !hasTrayItems)
            {
                return;
            }

            ConsumeContentsAndSync();
        }

        public void InitializeTray(string displayName, GameObject modelPrefab = null, bool isSterilized = false)
        {
            base.Initialize(displayName, modelPrefab);
            ResetTrayState(isSterilized);
        }

        private void EnsureTrayData()
        {
            if (_trayData == null)
            {
                _trayData = new SubmittedTray();
            }
        }

        private void ResetTrayState(bool isSterilized = false)
        {
            _trayData = new SubmittedTray();
            _trayData.SetTrayKind(isSterilized ? TrayKind.Sterilized : TrayKind.Normal);
            RefreshTrayVisual();
        }

        private void ResetTrayStateAndSync(bool isSterilized = false)
        {
            ResetTrayState(isSterilized);

            PhotonView photonView = PhotonView;
            if (photonView == null || !PhotonNetwork.InRoom || !photonView.IsMine)
            {
                return;
            }

            photonView.RPC(nameof(RPC_ResetTrayData), RpcTarget.Others, isSterilized);
        }

        public void ConsumeContentsAndSync()
        {
            bool isSterilized = IsSterilizedTray;

            // 트레이 Destroy 이벤트보다 먼저 도착하도록
            // 다른 클라이언트에서 자식 아이템을 분리합니다.
            int[] storedViewIds = _slots?.GetStoredItemViewIds();
            if (storedViewIds != null && storedViewIds.Length > 0 && PhotonNetwork.InRoom)
            {
                PhotonView pv = PhotonView;
                if (pv != null)
                {
                    pv.RPC(nameof(RPC_DetachStoredItems), RpcTarget.Others, storedViewIds);
                }
            }

            int[] undestroyedViewIds = _slots?.ClearStoredItems();
            ResetTrayStateAndSync(isSterilized);

            // 로컬에서 직접 회수하지 못한 아이템은 마스터에게 정리를 요청합니다.
            if (undestroyedViewIds != null && undestroyedViewIds.Length > 0 && PhotonNetwork.InRoom)
            {
                PhotonView pv = PhotonView;
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

        public void ApplyTraySnapshot(SubmittedTray trayData)
        {
            _trayData = trayData != null ? trayData.Clone() : new SubmittedTray();
            RefreshTrayVisual();
        }

        private void SetTrayKind(TrayKind trayKind)
        {
            EnsureTrayData();
            _trayData.SetTrayKind(trayKind);
            RefreshTrayVisual();
        }

        public int GetFirstAvailableSlotIndex()
        {
            return _slots != null ? _slots.GetFirstAvailableSlotIndex() : -1;
        }

        public bool CanStoreItem(CraftedItem item, int slotIndex = -1)
        {
            if (item == null || _slots == null)
            {
                return false;
            }

            EnsureTrayData();

            if (!_trayData.CanAddItem())
            {
                return false;
            }

            if (slotIndex >= 0)
            {
                return _slots.IsSlotAvailable(slotIndex);
            }

            return GetFirstAvailableSlotIndex() >= 0;
        }

        public bool TryStoreItem(ItemObject itemObject, CraftedItem item, int slotIndex)
        {
            if (itemObject == null || item == null || _slots == null)
            {
                return false;
            }

            EnsureTrayData();

            if (!CanStoreItem(item, slotIndex))
            {
                return false;
            }

            if (!_trayData.TryAddItem(item))
            {
                return false;
            }

            if (_slots.TryStoreItem(itemObject, slotIndex))
            {
                return true;
            }

            // 월드 배치가 실패하면 의미 데이터도 함께 되돌려 이중 상태가 벌어지지 않도록 맞춥니다.
            _trayData.TakeLastItem();
            return false;
        }

        public bool HasAnyItems()
        {
            EnsureTrayData();
            return _trayData.HasAnyItems();
        }

        public bool CanBeSterilized()
        {
            return !IsSterilizedTray && !HasAnyItems();
        }

        public void SetTrayKindAndSync(TrayKind trayKind)
        {
            SetTrayKind(trayKind);

            PhotonView photonView = PhotonView;
            if (photonView == null || !PhotonNetwork.InRoom || !photonView.IsMine)
            {
                return;
            }

            photonView.RPC(nameof(RPC_SetTrayKind), RpcTarget.Others, (int)trayKind);
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
            ResetTrayState(isSterilized);
        }

        public SubmittedTray GetTraySnapshot()
        {
            EnsureTrayData();
            return _trayData.Clone();
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            ResetSourceState();
            SetAsSupplyItem();
            ResetTrayState(false);
        }

        private void RefreshTrayVisual()
        {
            bool isSterilized = IsSterilizedTray;

            if (_normalVisual != null)
            {
                _normalVisual.SetActive(!isSterilized);
            }

            if (_sterilizedVisual != null)
            {
                _sterilizedVisual.SetActive(isSterilized);
            }
        }
    }
}
