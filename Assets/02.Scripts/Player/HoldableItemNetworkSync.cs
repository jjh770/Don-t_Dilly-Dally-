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

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _holdableItem = GetComponent<HoldableItem>();
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
            stream.SendNext(_holdableItem.IsInteracting);
            stream.SendNext(_holdableItem.HolderActorNumber);
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

        // 컨테이너(트레이, 머신 슬롯 등)에 적재된 아이템은 콜라이더를 다시 활성화하지 않음
        if (_holdableItem.IsStoredInContainer)
            return;

        _collider.enabled = !isHeld;
    }
}
