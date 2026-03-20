using DontDillyDally.Data;
using Photon.Pun;
using System.Collections;
using UnityEngine;

public class PlayerInteractionAbility : MonoBehaviour
{
    private const float HalfAngleMultiplier = 0.5f;
    private const float MinMoveSqrMagnitude = 0.01f;
    private const float DefaultSpeedMultiplier = 1f;
    private const int GizmoSegments = 20;
    private const int MaxDetectionColliders = 10;

    private readonly Collider[] _detectionColliders = new Collider[MaxDetectionColliders];

    [Header("아이템 탐지 설정")]
    [SerializeField] private float _detectionRadius = 2f;
    [SerializeField] private float _detectionAngle = 60f;
    [SerializeField] private float _detectionHeight = 1f;
    [SerializeField] private float _detectionHeightOffset = 0.5f;

    [Header("밀기 설정")]
    [SerializeField] private float _pushSpeedMultiplier = 0.5f;
    [SerializeField] private float _pushRotationMultiplier = 0.2f;

    [Header("던지기 설정")]
    [SerializeField] private float _throwForce = 5f;
    [SerializeField] private float _throwRotationSpeed = 20f;
    [SerializeField] private float _throwDelay = 0.2f;
    [SerializeField] private float _rotationAngleThreshold = 5f;
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private LayerMask _interactableLayer;
    public Transform HoldPoint => _holdPoint;
    public ItemObject CurrentHeldItem => _currentHeldItem;

    private IInteractable _currentInteractable;
    // 들고있는 아이템 판별
    private ItemObject _currentHeldItem;
    private IInteractable _nearestInteractable;

    private PlayerController _playerController;
    private PlayerAnimator _playerAnimator;
    private PlayerMovementAbility _playerMovement;
    private Camera _camera;
    private bool _isThrowing;
    private float _detectionAngleCos;
    private Collider[] _playerColliders;

    private IInteractable _pendingHoldInteractable;
    private ItemObject _pendingHeldItem;
    private NetworkItemOwnership _pendingOwnership;
    private bool _isExternalInteractionLocked;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _playerMovement = GetComponent<PlayerMovementAbility>();
        _camera = Camera.main;
        _detectionAngleCos = Mathf.Cos(_detectionAngle * HalfAngleMultiplier * Mathf.Deg2Rad);
        _playerColliders = GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (_playerController?.PhotonView != null && !_playerController.PhotonView.IsMine)
        {
            return;
        }

        TryCompletePendingHold();

        if (_isExternalInteractionLocked)
        {
            return;
        }

        FindNearestInteractable();
        HandleInteractInput();
        HandlePushableMovement();
    }

    private void FindNearestInteractable()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, _detectionColliders, _interactableLayer);

        _nearestInteractable = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = _detectionColliders[i];
            IInteractable interactable = TryResolvePriorityInteractable(col);
            if (interactable != null && !interactable.IsInteracting)
            {
                // 높이 체크
                float detectionCenterY = transform.position.y + _detectionHeightOffset;
                float heightDiff = Mathf.Abs(col.transform.position.y - detectionCenterY);
                if (heightDiff > _detectionHeight)
                {
                    continue;
                }

                // 시야각 체크
                Vector3 directionToItem = col.transform.position - transform.position;
                directionToItem.y = 0;
                float dot = Vector3.Dot(transform.forward, directionToItem.normalized);

                if (dot < _detectionAngleCos)
                {
                    continue;
                }

                float sqrDistance = (col.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    _nearestInteractable = interactable;
                }
            }
        }
    }

    private IInteractable TryResolvePriorityInteractable(Collider col)
    {
        if (col == null)
        {
            return null;
        }

        if (_currentHeldItem != null &&
            col.TryGetComponent(out SyringeFillTarget syringeFillTarget) &&
            syringeFillTarget.CanInteractWith(_currentHeldItem))
        {
            return syringeFillTarget;
        }

        if (col.TryGetComponent(out IInteractable interactable))
        {
            return interactable;
        }

        return null;
    }

    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_currentInteractable is IHoldable &&
                _nearestInteractable != null &&
                _nearestInteractable != _currentInteractable &&
                _nearestInteractable is not IHoldable)
            {
                StartInteract(_nearestInteractable);
                return;
            }

            if (_currentInteractable != null)
            {
                StopInteract();
            }
            else if (_nearestInteractable != null)
            {
                StartInteract(_nearestInteractable);
            }
        }

        // Holdable 전용: 던지기
        if (Input.GetMouseButtonDown(0))
        {
            if (_currentInteractable is not IHoldable) return;
            if (_isThrowing) return;

            StartCoroutine(ThrowItemCoroutine());
        }
    }

    private void StartInteract(IInteractable interactable)
    {
        if (_currentInteractable is IHoldable && interactable is IHoldable)
            return;

        if (interactable is IHoldable)
        {
            TryStartHold(interactable);
            return;
        }

        if (interactable is IPushable)
        {
            interactable.Interact(transform);
            _currentInteractable = interactable;
            _playerAnimator.PlayGrabAnimation(true);
            _playerMovement.SetSpeedMultiplier(_pushSpeedMultiplier, _pushRotationMultiplier);
            return;
        }

        interactable.Interact(transform);
    }

    private bool TryStartHold(IInteractable interactable)
    {
        if (interactable is not IHoldable holdable)
            return false;

        if (!TryResolveHeldItem(interactable, out ItemObject itemObject))
            return false;

        NetworkItemOwnership ownership = itemObject.NetworkOwnership;
        if (ownership == null)
            return false;

        if (ownership.IsOwnedLocally)
        {
            BeginHold(interactable, holdable, itemObject);
            return true;
        }

        _pendingHoldInteractable = interactable;
        _pendingHeldItem = itemObject;
        _pendingOwnership = ownership;

        ownership.TryAcquireOrRequestOwnership();
        return false;
    }

    private void BeginHold(IInteractable interactable, IHoldable holdable, ItemObject itemObject)
    {
        holdable.Interact(_holdPoint, PhotonNetwork.LocalPlayer.ActorNumber);
        itemObject.NetworkOwnership?.BeginHold(PhotonNetwork.LocalPlayer.ActorNumber);
        itemObject.NotifyLeftSource();

        _currentInteractable = interactable;
        _currentHeldItem = itemObject;

        _pendingHoldInteractable = null;
        _pendingHeldItem = null;
        _pendingOwnership = null;

        _playerAnimator.PlayHoldAnimation(true);
    }

    private bool TryResolveHeldItem(IInteractable interactable, out ItemObject itemObject)
    {
        itemObject = null;

        if (interactable is not Component component)
            return false;

        itemObject = component.GetComponent<ItemObject>();
        return itemObject != null;
    }

    private bool TryAcquireItemOwnership(ItemObject itemObject)
    {
        PhotonView photonView = itemObject.GetComponent<PhotonView>();
        if (photonView == null || PhotonNetwork.LocalPlayer == null)
            return false;

        if (photonView.IsMine)
            return true;

        photonView.RequestOwnership();
        return false;
    }

    private void TryCompletePendingHold()
    {
        if (_pendingHoldInteractable is not IHoldable holdable)
            return;

        if (_pendingHeldItem == null || _pendingOwnership == null)
            return;

        if (!_pendingOwnership.IsOwnedLocally)
            return;

        BeginHold(_pendingHoldInteractable, holdable, _pendingHeldItem);
    }

    private void StopInteract()
    {
        if (_currentInteractable is IHoldable holdable)
        {
            _currentHeldItem?.NetworkOwnership?.EndHold();
            holdable.StopInteract();

            ReleaseHeldItemOwnershipToMaster();

            _playerAnimator.PlayHoldAnimation(false);
            _currentInteractable = null;
            _currentHeldItem = null;
            return;
        }

        if (_currentInteractable is IPushable)
        {
            _currentInteractable.StopInteract();
            _playerAnimator.PlayGrabAnimation(false);
            _playerAnimator.PlayPushAnimation(false);
            _playerMovement.SetSpeedMultiplier(DefaultSpeedMultiplier, DefaultSpeedMultiplier);
        }

        _currentInteractable = null;
    }

    private void ReleaseHeldItemOwnershipToMaster()
    {
        if (_currentHeldItem == null)
            return;

        PhotonView photonView = _currentHeldItem.GetComponent<PhotonView>();
        if (photonView == null)
            return;

        if (PhotonNetwork.MasterClient == null)
            return;

        photonView.TransferOwnership(PhotonNetwork.MasterClient);
    }

    public bool TryConsumeHeldItem(ItemObject expectedItem = null)
    {
        if (_currentHeldItem == null)
            return false;

        if (expectedItem != null && _currentHeldItem != expectedItem)
            return false;

        if (_currentInteractable is not IHoldable holdable)
            return false;

        ItemObject heldItem = _currentHeldItem;
        _currentHeldItem?.NetworkOwnership?.EndHold();
        holdable.StopInteract();

        _playerAnimator.PlayHoldAnimation(false);
        _currentInteractable = null;
        _currentHeldItem = null;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.Destroy(heldItem.gameObject);
        }
        else
        {
            Destroy(heldItem.gameObject);
        }

        return true;
    }

    public bool TryReleaseHeldItem(ItemObject expectedItem = null, bool returnOwnershipToMaster = true)
    {
        if (_currentHeldItem == null)
            return false;

        if (expectedItem != null && _currentHeldItem != expectedItem)
            return false;

        if (_currentInteractable is not IHoldable holdable)
            return false;

        _currentHeldItem?.NetworkOwnership?.EndHold();
        holdable.StopInteract();

        if (returnOwnershipToMaster)
            ReleaseHeldItemOwnershipToMaster();

        _playerAnimator.PlayHoldAnimation(false);
        _currentInteractable = null;
        _currentHeldItem = null;
        return true;
    }

    public bool TryStartHoldFromExternal(IInteractable interactable)
    {
        if (interactable == null)
        {
            return false;
        }

        if (_currentInteractable != null || _currentHeldItem != null)
        {
            return false;
        }

        if (interactable is not IHoldable)
        {
            return false;
        }

        return TryStartHold(interactable);
    }

    public bool TryBeginExternalInteractionLock(ItemObject expectedHeldItem)
    {
        if (_isExternalInteractionLocked)
        {
            return false;
        }

        if (_currentHeldItem == null || _currentHeldItem != expectedHeldItem)
        {
            return false;
        }

        if (_currentInteractable is not IHoldable)
        {
            return false;
        }

        _isExternalInteractionLocked = true;
        _playerMovement.SetMovementLocked(true);
        return true;
    }

    public void EndExternalInteractionLock()
    {
        _isExternalInteractionLocked = false;
        _playerMovement.SetMovementLocked(false);
    }

    private void HandlePushableMovement()
    {
        if (_currentInteractable is not IPushable) return;

        Vector3 moveDirection = _playerMovement.MoveDirection;
        bool isMoving = moveDirection.sqrMagnitude > MinMoveSqrMagnitude;
        _playerAnimator.PlayPushAnimation(isMoving);
    }

    private IEnumerator ThrowItemCoroutine()
    {
        _isThrowing = true;

        Vector3 throwDirection = GetMouseWorldDirection();
        Quaternion targetRotation = Quaternion.LookRotation(throwDirection);

        // 캐릭터 회전
        while (Quaternion.Angle(transform.rotation, targetRotation) > _rotationAngleThreshold)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _throwRotationSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRotation;

        // 던지기
        if (_currentInteractable is not IHoldable holdable)
        {
            _isThrowing = false;
            yield break;
        }

        _playerAnimator.PlayThrowAnimation();
        yield return new WaitForSeconds(_throwDelay);
        _currentHeldItem?.NetworkOwnership?.EndHold();
        holdable.Throw(throwDirection, _throwForce, _playerColliders);

        _currentInteractable = null;
        _currentHeldItem = null;

        // 잡는 애니메이션 취소
        _playerAnimator.ResetThrowAnimation();
        _playerAnimator.PlayHoldAnimation(false);

        _isThrowing = false;
    }

    private Vector3 GetMouseWorldDirection()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 direction = (hitPoint - transform.position).normalized;
            return direction;
        }

        return transform.forward;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        float halfAngle = _detectionAngle * HalfAngleMultiplier;
        Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * transform.forward;

        Vector3 centerOffset = Vector3.up * _detectionHeightOffset;
        Vector3 bottomOffset = centerOffset + Vector3.down * _detectionHeight;
        Vector3 topOffset = centerOffset + Vector3.up * _detectionHeight;

        // 상단/하단 시야각 경계선
        DrawFanAtHeight(topOffset, leftDir, rightDir, halfAngle);
        DrawFanAtHeight(bottomOffset, leftDir, rightDir, halfAngle);

        // 수직 경계선
        Gizmos.DrawLine(transform.position + topOffset, transform.position + bottomOffset);
        Gizmos.DrawLine(transform.position + leftDir * _detectionRadius + topOffset, transform.position + leftDir * _detectionRadius + bottomOffset);
        Gizmos.DrawLine(transform.position + rightDir * _detectionRadius + topOffset, transform.position + rightDir * _detectionRadius + bottomOffset);
    }

    private void DrawFanAtHeight(Vector3 heightOffset, Vector3 leftDir, Vector3 rightDir, float halfAngle)
    {
        Vector3 origin = transform.position + heightOffset;

        Gizmos.DrawLine(origin, origin + leftDir * _detectionRadius);
        Gizmos.DrawLine(origin, origin + rightDir * _detectionRadius);

        int segments = GizmoSegments;
        float angleStep = _detectionAngle / segments;
        Vector3 prevPoint = origin + leftDir * _detectionRadius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -halfAngle + angleStep * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            Vector3 point = origin + dir * _detectionRadius;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}
