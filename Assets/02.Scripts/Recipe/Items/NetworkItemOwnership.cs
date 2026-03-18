using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(NetworkItemState))]
    public class NetworkItemOwnership : MonoBehaviour, IPunOwnershipCallbacks
    {
        private PhotonView _photonView;
        private NetworkItemState _itemState;
        private ItemObject _itemObject;
        private bool _isOwnershipRequestPending;

        public event Action<NetworkItemOwnership> OwnershipAcquiredLocally;

        public bool HasLeftSource => _itemState != null && _itemState.HasLeftSource;
        public bool IsHeld => _itemState != null && _itemState.IsHeld;
        public int HolderActorNumber => _itemState != null ? _itemState.HolderActorNumber : -1;
        public bool IsOwnedLocally => _photonView != null && _photonView.IsMine;
        public PhotonView PhotonView => _photonView;
        public NetworkItemState State => _itemState;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
            _itemState = GetComponent<NetworkItemState>();
            _itemObject = GetComponent<ItemObject>();

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

        public void BeginHold(int holderActorNumber)
        {
            _itemState?.BeginHold(holderActorNumber);
            _itemObject?.SetAsInteractableItem();

            if (_photonView != null && PhotonNetwork.InRoom)
                _photonView.RPC(nameof(RPC_SetAsInteractableItem), RpcTarget.Others);
        }

        public void EndHold()
        {
            _itemState?.EndHold();
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

            targetView.TransferOwnership(requestingPlayer);
        }

        public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
        {
            if (targetView != _photonView)
                return;

            _isOwnershipRequestPending = false;

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
