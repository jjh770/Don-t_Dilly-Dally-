using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(HoldableItem))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PhotonView))]
public class HoldableItemNetworkSync : MonoBehaviour, IPunOwnershipCallbacks
{
    [Header("Lerp 속도")]
    [SerializeField] private float _positionLerpSpeed = 10f;
    [SerializeField] private float _rotationLerpSpeed = 10f;

    private PhotonView _photonView;
    private Rigidbody _rigidbody;
    private HoldableItem _holdableItem;

    private Vector3 _networkPosition;
    private Quaternion _networkRotation;
    private bool _firstReceive;
    private bool _wasStored;
    private bool _snapOnNextReceive;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
        _holdableItem = GetComponent<HoldableItem>();
    }

    private void OnEnable()
    {
        // 풀 재사용 시 네트워크 보간 상태를 깨끗이 초기화합니다.
        // Awake 의존 필드가 없어야 (0,0,0) stale 위치 버그가 발생하지 않습니다.
        _firstReceive = true;
        _wasStored = _holdableItem != null && _holdableItem.IsStoredInContainer;
        _snapOnNextReceive = false;
        _networkPosition = transform.position;
        _networkRotation = transform.rotation;

        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void Update()
    {
        if (_photonView == null || _photonView.IsMine)
            return;

        if (_holdableItem.IsStoredInContainer)
            return;

        if (_firstReceive)
            return;

        transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * _positionLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, Time.deltaTime * _rotationLerpSpeed);
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
            stream.SendNext(_holdableItem.IsStoredInContainer);

            // 컨테이너(트레이/머신) 슬롯에 담긴 아이템은 parent의 local 좌표가 권위이므로
            // 월드 위치를 보내지 않습니다. 담긴 상태→꺼낸 상태로 전환되는 첫 패킷이 들어올 때
            // 하드 스냅으로 복귀합니다.
            if (!_holdableItem.IsStoredInContainer)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            return;
        }

        bool networkIsHeld = (bool)stream.ReceiveNext();
        int networkHolderActorNumber = (int)stream.ReceiveNext();
        bool networkIsStoredInContainer = (bool)stream.ReceiveNext();

        if (!networkIsStoredInContainer)
        {
            _networkPosition = (Vector3)stream.ReceiveNext();
            _networkRotation = (Quaternion)stream.ReceiveNext();

            bool becameUnstored = _wasStored;
            bool shouldSnap = becameUnstored || _snapOnNextReceive;
            if (_firstReceive && !shouldSnap && IsNearWorldOrigin(_networkPosition) && !IsNearWorldOrigin(transform.position))
            {
                int vid = _photonView != null ? _photonView.ViewID : -1;
                Debug.LogWarning($"[HoldableSync] Ignored first near-origin transform vid={vid} net={_networkPosition} current={transform.position}");
                _networkPosition = transform.position;
                _networkRotation = transform.rotation;
            }

            if (shouldSnap)
            {
                transform.SetPositionAndRotation(_networkPosition, _networkRotation);

                if (_networkPosition.sqrMagnitude < 4.0f)
                {
                    int vid = _photonView != null ? _photonView.ViewID : -1;
                    Debug.LogWarning($"[HoldableSync] HardSnap near-origin vid={vid} net={_networkPosition} becameUnstored={becameUnstored} snapOnNext={_snapOnNextReceive}");
                }
            }

            _firstReceive = false;
            _snapOnNextReceive = false;
        }

        _wasStored = networkIsStoredInContainer;
        ApplyRemoteHeldState(networkIsHeld, networkHolderActorNumber, networkIsStoredInContainer);
    }

    private void ApplyRemoteHeldState(bool isHeld, int holderActorNumber, bool isStoredInContainer)
    {
        if (_photonView != null && _photonView.IsMine)
            return;

        _holdableItem.ApplyNetworkContainerState(isStoredInContainer);

        if (isStoredInContainer)
        {
            _rigidbody.isKinematic = true;
            _holdableItem.SetAllCollidersEnabled(false);
            return;
        }

        _holdableItem.ApplyNetworkHoldState(isHeld, holderActorNumber);
        _rigidbody.isKinematic = true;

        // 컨테이너(트레이, 머신 슬롯 등)에 적재된 아이템은 콜라이더를 다시 활성화하지 않음
        if (_holdableItem.IsStoredInContainer)
            return;

        _holdableItem.SetAllCollidersEnabled(!isHeld);
    }

    private static bool IsNearWorldOrigin(Vector3 position)
    {
        return Mathf.Abs(position.x) < 0.5f &&
               Mathf.Abs(position.z) < 0.5f &&
               Mathf.Abs(position.y) < 1f;
    }

    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        // 소유권 요청은 NetworkItemOwnership가 처리. 여기서는 no-op.
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if (targetView != _photonView)
            return;

        // 소유권이 넘어가면 다른 클라이언트에서는 새 소유자의 첫 패킷을 하드 스냅으로 받도록 리셋.
        // 새 소유자 본인은 _photonView.IsMine이므로 Update/Serialize 읽기 경로가 안 돌아감.
        _firstReceive = true;
        _snapOnNextReceive = true;
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
        // no-op
    }
}
