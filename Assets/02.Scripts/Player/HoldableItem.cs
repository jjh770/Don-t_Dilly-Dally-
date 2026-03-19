using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(HoldableItemNetworkSync))]
public class HoldableItem : MonoBehaviour, IHoldable, IPunObservable
{
    public bool IsInteracting { get; private set; }
    public Transform Transform => transform;

    [Header("착지 감지 설정")]
    [SerializeField] private float _settleVelocityThreshold = 0.05f;
    [SerializeField] private float _settleAngularVelocityThreshold = 0.05f;
    [SerializeField] private float _settleCheckDelay = 0.1f;
    [SerializeField] private float _settleRequiredDuration = 0.25f;

    private bool _isWaitingForOwnershipReturn;
    private float _settledTime;

    [Header("던지기 설정")]
    [SerializeField] private float _upAngle = 0.5f;
    [SerializeField] private float _ignoreCollisionDuration = 0.3f;

    private Rigidbody _rigidbody;
    private Collider _collider;
    private PhotonView _photonView;
    private HoldableItemNetworkSync _networkSync;
    private Transform _currentHoldPoint;

    // 누가 들었는가 확인용
    private const int InvalidActorNumber = -1;
    private int _holderActorNumber = InvalidActorNumber;

    public int HolderActorNumber => _holderActorNumber;
    public bool HasHolder => _holderActorNumber != InvalidActorNumber;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _photonView = GetComponent<PhotonView>();
        _networkSync = GetComponent<HoldableItemNetworkSync>();

        if (_networkSync == null)
            _networkSync = gameObject.AddComponent<HoldableItemNetworkSync>();
    }

    private void LateUpdate()
    {
        UpdateHeldTransform();
        UpdateOwnershipReturn();
    }

    private void FixedUpdate()
    {
        _networkSync?.EnforceRemotePhysicsAuthority();
    }

    private void UpdateHeldTransform()
    {
        if (!IsInteracting)
            return;

        Transform targetHoldPoint = _currentHoldPoint;

        if (_photonView != null && !_photonView.IsMine)
        {
            targetHoldPoint = TryResolveHoldPoint(_holderActorNumber);
        }

        if (targetHoldPoint == null)
            return;

        transform.SetPositionAndRotation(targetHoldPoint.position, targetHoldPoint.rotation);
    }

    private void UpdateOwnershipReturn()
    {
        if (!_isWaitingForOwnershipReturn)
            return;

        if (_photonView == null || !_photonView.IsMine)
            return;

        if (PhotonNetwork.MasterClient == null)
            return;

        bool isLinearSettled =
            _rigidbody.linearVelocity.sqrMagnitude <= _settleVelocityThreshold * _settleVelocityThreshold;
        bool isAngularSettled =
            _rigidbody.angularVelocity.sqrMagnitude <= _settleAngularVelocityThreshold * _settleAngularVelocityThreshold;

        if (isLinearSettled && isAngularSettled)
        {
            _settledTime += Time.deltaTime;

            if (_settledTime >= _settleRequiredDuration)
            {
                _isWaitingForOwnershipReturn = false;
                _settledTime = 0f;
                _photonView.TransferOwnership(PhotonNetwork.MasterClient);
            }
        }
        else
        {
            _settledTime = 0f;
        }
    }

    public void Interact(Transform holdPoint)
    {
        int actorNumber = PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : InvalidActorNumber;

        Interact(holdPoint, actorNumber);
    }

    public void Interact(Transform holdPoint, int holderActorNumber)
    {
        _isWaitingForOwnershipReturn = false;
        _settledTime = 0f;

        IsInteracting = true;
        _currentHoldPoint = holdPoint;
        _holderActorNumber = holderActorNumber;

        StopDynamicMotion();
        _rigidbody.isKinematic = true;
        _collider.enabled = false;

        transform.SetParent(null);
        transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
    }

    public void StopInteract()
    {
        Drop();
    }

    public void Hold(Transform holdPoint)
    {
        Interact(holdPoint);
    }

    public void Throw(Vector3 direction, float force, Collider[] throwerColliders = null)
    {
        IsInteracting = false;
        _currentHoldPoint = null;

        transform.SetParent(null);
        _rigidbody.isKinematic = false;
        _collider.enabled = true;

        if (throwerColliders != null)
        {
            StartCoroutine(IgnoreCollisionTemporarily(throwerColliders));
        }

        Vector3 throwDirection = (direction + Vector3.up * _upAngle).normalized;
        _rigidbody.AddForce(throwDirection * force, ForceMode.Impulse);

        _isWaitingForOwnershipReturn = true;
        _settledTime = 0f;

        _holderActorNumber = InvalidActorNumber;
    }

    public void Place(Transform placePoint)
    {
        if (placePoint == null)
            return;

        _isWaitingForOwnershipReturn = false;
        _settledTime = 0f;

        IsInteracting = false;
        _currentHoldPoint = null;
        _holderActorNumber = InvalidActorNumber;

        transform.SetParent(null);
        StopDynamicMotion();
        _rigidbody.isKinematic = true;
        _collider.enabled = true;
        transform.SetPositionAndRotation(placePoint.position, placePoint.rotation);
    }

    private void StopDynamicMotion()
    {
        if (_rigidbody == null || _rigidbody.isKinematic)
            return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }


    private IEnumerator IgnoreCollisionTemporarily(Collider[] colliders)
    {
        // 던지자마자 잠깐 플레이어 콜라이더 무시 (충돌 안 하게)
        SetCollisionWithThrower(colliders, true);

        yield return new WaitForSeconds(_ignoreCollisionDuration);

        SetCollisionWithThrower(colliders, false);
    }

    private void SetCollisionWithThrower(Collider[] colliders, bool Isignore)
    {
        if (_collider == null) return;

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                Physics.IgnoreCollision(_collider, col, Isignore);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        _networkSync?.Serialize(stream, info);
    }

    public void ApplyNetworkHoldState(bool isHeld, int holderActorNumber)
    {
        if (_photonView != null && _photonView.IsMine)
            return;

        IsInteracting = isHeld;
        _holderActorNumber = holderActorNumber;

        if (!isHeld)
            _currentHoldPoint = null;
    }

    public void Drop()
    {
        _isWaitingForOwnershipReturn = false;
        _settledTime = 0f;

        IsInteracting = false;
        _currentHoldPoint = null;

        transform.SetParent(null);
        _rigidbody.isKinematic = false;
        _collider.enabled = true;
        _holderActorNumber = InvalidActorNumber;
    }

    private Transform TryResolveHoldPoint(int holderActorNumber)
    {
        if (holderActorNumber == InvalidActorNumber)
            return null;

        if (!PlayerRegistry.TryGetPlayer(holderActorNumber, out PlayerController player))
            return null;

        return player.GetHoldPoint();
    }
}
