using DontDillyDally.Data;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(HoldableItem))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PhotonView))]
public class HoldableItemNetworkSync : MonoBehaviour
{
    private PhotonView _photonView;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private HoldableItem _holdableItem;
    private NetworkItemOwnership _networkOwnership;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _holdableItem = GetComponent<HoldableItem>();
        _networkOwnership = GetComponent<NetworkItemOwnership>();
    }

    public void EnforceRemotePhysicsAuthority()
    {
        if (_photonView == null || _photonView.IsMine)
            return;

        if (!_rigidbody.isKinematic)
            _rigidbody.isKinematic = true;
    }

    public void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            bool isHeld = _networkOwnership != null && _networkOwnership.IsHeld;
            int holderActorNumber = _networkOwnership != null
                ? _networkOwnership.HolderActorNumber
                : _holdableItem.HolderActorNumber;

            stream.SendNext(isHeld);
            stream.SendNext(holderActorNumber);
            return;
        }

        bool networkIsHeld = (bool)stream.ReceiveNext();
        int networkHolderActorNumber = (int)stream.ReceiveNext();
        ApplyRemoteHeldState(networkIsHeld, networkHolderActorNumber);
    }

    private void ApplyRemoteHeldState(bool isHeld, int holderActorNumber)
    {
        if (_photonView != null && _photonView.IsMine)
            return;

        _holdableItem.ApplyNetworkHoldState(isHeld, holderActorNumber);
        _rigidbody.isKinematic = true;
        _collider.enabled = !isHeld;
    }
}
