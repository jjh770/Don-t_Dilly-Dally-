using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(NetworkItemState))]
    public class NetworkItemOwnership : MonoBehaviour, IPunOwnershipCallbacks
    {
        private const int NoActorNumber = -1;

        private PhotonView _photonView;
        private NetworkItemState _itemState;
        private ItemObject _itemObject;
        private HoldableItem _holdableItem;
        private bool _isOwnershipRequestPending;
        private int _grantedOwnerActorNumber = NoActorNumber;

        public event Action<NetworkItemOwnership> OwnershipAcquiredLocally;

        public bool HasLeftSource => _itemState != null && _itemState.HasLeftSource;
        public bool IsHeld => _holdableItem != null && _holdableItem.IsInteracting;
        public int HolderActorNumber => _holdableItem != null ? _holdableItem.HolderActorNumber : -1;
        public bool IsOwnedLocally => _photonView != null && _photonView.IsMine;
        public PhotonView PhotonView => _photonView;
        public NetworkItemState State => _itemState;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
            _itemState = GetComponent<NetworkItemState>();
            _itemObject = GetComponent<ItemObject>();
            _holdableItem = GetComponent<HoldableItem>();

            if (_itemState == null)
                _itemState = gameObject.AddComponent<NetworkItemState>();
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        /// <summary>
        /// Controller(Master)가 자신의 소유권을 사용 중으로 잠급니다.
        /// 잠금 중에는 다른 플레이어의 소유권 요청이 거부됩니다.
        /// </summary>
        public void LockOwnershipOnController()
        {
            if (_photonView != null && _photonView.AmController && _photonView.Owner != null)
            {
                _grantedOwnerActorNumber = _photonView.Owner.ActorNumber;
            }
        }

        public void UnlockOwnershipOnController()
        {
            if (_photonView != null && _photonView.AmController)
            {
                _grantedOwnerActorNumber = NoActorNumber;
            }
        }

        public bool TryAcquireOrRequestOwnership()
        {
            if (_photonView == null || PhotonNetwork.LocalPlayer == null)
                return false;

            if (_photonView.IsMine)
                return true;

            if (_isOwnershipRequestPending)
                return false;

            _isOwnershipRequestPending = true;
            _photonView.RequestOwnership();
            return false;
        }

        public void NotifyHoldStarted()
        {
            _itemObject?.SetAsInteractableItem();

            if (_photonView != null && PhotonNetwork.InRoom)
                _photonView.RPC(nameof(RPC_SetAsInteractableItem), RpcTarget.Others);
        }

        public void MarkLeftSource()
        {
            _itemState?.MarkLeftSource();
        }

        public void ResetSourceState()
        {
            _itemState?.ResetSourceState();
        }

        public void NotifyLeftSource()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                MarkLeftSource();
                return;
            }

            if (_photonView == null)
                return;

            _photonView.RPC(nameof(RPC_MarkLeftSource), RpcTarget.MasterClient);
        }

        [PunRPC]
        public void RPC_MarkLeftSource()
        {
            MarkLeftSource();
        }

        [PunRPC]
        private void RPC_SetAsInteractableItem()
        {
            _itemObject?.SetAsInteractableItem();
        }

        public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
        {
            if (targetView != _photonView)
                return;

            if (!_photonView.AmController)
                return;

            if (IsHeld && requestingPlayer.ActorNumber != HolderActorNumber)
                return;

            // 소유권이 부여되었지만 hold가 아직 확인되지 않은 경우, 다른 플레이어의 요청 거부
            if (_grantedOwnerActorNumber != NoActorNumber && !IsHeld)
            {
                // 부여 대상이 더 이상 소유자가 아니면 (소유권 반환됨) 초기화
                if (_photonView.Owner == null || _photonView.Owner.ActorNumber != _grantedOwnerActorNumber)
                {
                    _grantedOwnerActorNumber = NoActorNumber;
                }
                else if (requestingPlayer.ActorNumber != _grantedOwnerActorNumber)
                {
                    return;
                }
            }

            _grantedOwnerActorNumber = requestingPlayer.ActorNumber;
            targetView.TransferOwnership(requestingPlayer);
        }

        public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
        {
            if (targetView != _photonView)
                return;

            _isOwnershipRequestPending = false;

            // 소유권이 Controller(Master)에게 돌아오면 부여 추적 초기화
            if (_photonView.AmController && _photonView.IsMine)
            {
                _grantedOwnerActorNumber = NoActorNumber;
            }

            if (_photonView.IsMine)
                OwnershipAcquiredLocally?.Invoke(this);
        }

        public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
        {
            if (targetView != _photonView)
                return;

            if (PhotonNetwork.LocalPlayer != null &&
                senderOfFailedRequest != null &&
                senderOfFailedRequest.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                _isOwnershipRequestPending = false;
            }
        }
    }
}
