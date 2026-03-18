using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(PhotonView))]
    public class NetworkItemOwnership : MonoBehaviour, IPunOwnershipCallbacks
    {
        private const int InvalidActorNumber = -1;

        private PhotonView _photonView;
        private bool _hasLeftSource;
        private bool _isHeld;
        private int _holderActorNumber = InvalidActorNumber;
        private bool _isOwnershipRequestPending;

        public event Action<NetworkItemOwnership> OwnershipAcquiredLocally;

        public bool HasLeftSource => _hasLeftSource;
        public bool IsHeld => _isHeld;
        public int HolderActorNumber => _holderActorNumber;
        public bool IsOwnedLocally => _photonView != null && _photonView.IsMine;
        public PhotonView PhotonView => _photonView;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
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
            _isHeld = true;
            _holderActorNumber = holderActorNumber;
        }

        public void EndHold()
        {
            _isHeld = false;
            _holderActorNumber = InvalidActorNumber;
        }

        public void MarkLeftSource()
        {
            _hasLeftSource = true;
        }

        public void ResetSourceState()
        {
            _hasLeftSource = false;
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

        public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
        {
            if (targetView != _photonView)
                return;

            if (!_photonView.AmController)
                return;

            if (_isHeld && requestingPlayer.ActorNumber != _holderActorNumber)
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
