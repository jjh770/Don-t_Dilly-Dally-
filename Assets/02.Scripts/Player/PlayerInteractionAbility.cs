using DontDillyDally.Data;
using System;
using UnityEngine;

[RequireComponent(typeof(PlayerHeldItemController))]
public class PlayerInteractionAbility : MonoBehaviour
{
    private const float HalfAngleMultiplier = 0.5f;
    private const float MinMoveSqrMagnitude = 0.01f;
    private const float DefaultSpeedMultiplier = 1f;

    private const int GizmoSegments = 20;
    private const int MaxDetectionColliders = 10;

    private readonly Collider[] _detectionColliders = new Collider[MaxDetectionColliders];

    [Header("아이템 감지 설정")]
    [SerializeField] private float _detectionRadius = 2f;
    [SerializeField] private float _detectionAngle = 60f;
    [SerializeField] private float _detectionHeight = 1f;
    [SerializeField] private float _detectionHeightOffset = 0.5f;

    [Header("밀기 설정")]
    [SerializeField] private float _pushSpeedMultiplier = 0.5f;
    [SerializeField] private float _pushRotationMultiplier = 0.2f;

    [SerializeField] private LayerMask _interactableLayer;

    public ItemObject CurrentHeldItem => _heldItemController != null ? _heldItemController.CurrentHeldItem : null;

    private IInteractable _currentPushInteractable;
    private IInteractable _nearestInteractable;
    private IInteractable _previousNearestInteractable;

    public event Action<IInteractable, IInteractable> OnNearestInteractableChanged;
    public event Action<ItemObject> OnHeldItemChanged;

    private PlayerController _playerController;
    private PlayerAnimator _playerAnimator;
    private PlayerMovementAbility _playerMovement;
    private PlayerHeldItemController _heldItemController;
    private float _detectionAngleCos;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _playerMovement = GetComponent<PlayerMovementAbility>();
        _heldItemController = GetComponent<PlayerHeldItemController>();

        _detectionAngleCos = Mathf.Cos(_detectionAngle * HalfAngleMultiplier * Mathf.Deg2Rad);
    }

    private void OnEnable()
    {
        _heldItemController.HeldItemChanged += HandleHeldItemChanged;
    }

    private void OnDisable()
    {
        _heldItemController.HeldItemChanged -= HandleHeldItemChanged;
    }

    private void Update()
    {
        if (_playerController?.PhotonView != null && !_playerController.PhotonView.IsMine)
        {
            return;
        }

        if (_heldItemController != null && _heldItemController.IsExternalInteractionLocked)
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

        IInteractable nearestCompatible = null;
        IInteractable nearestGeneral = null;
        float compatibleSqrDist = float.MaxValue;
        float generalSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = _detectionColliders[i];
            IInteractable interactable = TryResolveInteractable(col);
            if (interactable == null || interactable.IsInteracting)
            {
                continue;
            }

            Vector3 closestPoint = col.ClosestPoint(transform.position);

            float detectionCenterY = transform.position.y + _detectionHeightOffset;
            float heightDiff = Mathf.Abs(closestPoint.y - detectionCenterY);
            if (heightDiff > _detectionHeight)
            {
                continue;
            }

            Vector3 directionToItem = closestPoint - transform.position;
            directionToItem.y = 0;

            if (directionToItem.sqrMagnitude > 0.001f)
            {
                float dot = Vector3.Dot(transform.forward, directionToItem.normalized);
                if (dot < _detectionAngleCos)
                {
                    continue;
                }
            }

            float sqrDistance = (closestPoint - transform.position).sqrMagnitude;

            bool isCompatible = CurrentHeldItem != null
                && interactable is IItemAcceptor acceptor
                && acceptor.CanAcceptItem(CurrentHeldItem);

            if (isCompatible && sqrDistance < compatibleSqrDist)
            {
                compatibleSqrDist = sqrDistance;
                nearestCompatible = interactable;
            }

            if (sqrDistance < generalSqrDist)
            {
                generalSqrDist = sqrDistance;
                nearestGeneral = interactable;
            }
        }

        _nearestInteractable = nearestCompatible ?? nearestGeneral;
        NotifyNearestInteractableChanged();
    }

    private void NotifyNearestInteractableChanged()
    {
        if (_nearestInteractable == _previousNearestInteractable)
        {
            return;
        }

        OnNearestInteractableChanged?.Invoke(_previousNearestInteractable, _nearestInteractable);
        _previousNearestInteractable = _nearestInteractable;
    }

    private IInteractable TryResolveInteractable(Collider col)
    {
        if (col == null)
        {
            return null;
        }

        if (CurrentHeldItem != null &&
            col.TryGetComponent(out IItemAcceptor acceptor) &&
            acceptor is IInteractable acceptorInteractable)
        {
            return acceptorInteractable;
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
            if (_heldItemController != null && _heldItemController.IsThrowing)
            {
                return;
            }

            if (_heldItemController != null &&
                _heldItemController.IsHoldingHoldable &&
                _nearestInteractable != null &&
                _nearestInteractable != _heldItemController.CurrentHeldInteractable &&
                _nearestInteractable is not IHoldable)
            {
                StartInteract(_nearestInteractable);
                return;
            }

            if (_heldItemController != null && _heldItemController.IsHoldingHoldable)
            {
                _heldItemController.TryReleaseHeldItem();
            }
            else if (_currentPushInteractable != null)
            {
                StopInteract();
            }
            else if (_nearestInteractable != null)
            {
                StartInteract(_nearestInteractable);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            _heldItemController?.TryBeginThrow();
        }
    }

    private void StartInteract(IInteractable interactable)
    {
        if (_heldItemController != null && _heldItemController.IsHoldingHoldable && interactable is IHoldable)
        {
            return;
        }

        if (interactable is IHoldable)
        {
            _heldItemController?.TryPickupInteractable(interactable);
            return;
        }

        if (interactable is IPushable)
        {
            interactable.Interact(transform);
            _currentPushInteractable = interactable;
            _playerAnimator.PlayGrabAnimation(true);
            _playerMovement.SetSpeedMultiplier(_pushSpeedMultiplier, _pushRotationMultiplier);
            return;
        }

        interactable.Interact(transform);
    }

    private void StopInteract()
    {
        if (_heldItemController != null && _heldItemController.IsHoldingHoldable)
        {
            _heldItemController.TryReleaseHeldItem();
            return;
        }

        if (_currentPushInteractable is IPushable)
        {
            _currentPushInteractable.StopInteract();
            _playerAnimator.PlayGrabAnimation(false);
            _playerAnimator.PlayPushAnimation(false);
            _playerMovement.SetSpeedMultiplier(DefaultSpeedMultiplier, DefaultSpeedMultiplier);
        }

        _currentPushInteractable = null;
    }

    private void HandlePushableMovement()
    {
        if (_currentPushInteractable is not IPushable)
        {
            return;
        }

        Vector3 moveDirection = _playerMovement.MoveDirection;
        bool isMoving = moveDirection.sqrMagnitude > MinMoveSqrMagnitude;
        _playerAnimator.PlayPushAnimation(isMoving);
    }

    private void HandleHeldItemChanged(ItemObject heldItem)
    {
        OnHeldItemChanged?.Invoke(heldItem);
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

        DrawFanAtHeight(topOffset, leftDir, rightDir, halfAngle);
        DrawFanAtHeight(bottomOffset, leftDir, rightDir, halfAngle);

        Gizmos.DrawLine(transform.position + topOffset, transform.position + bottomOffset);
        Gizmos.DrawLine(transform.position + leftDir * _detectionRadius + topOffset, transform.position + leftDir * _detectionRadius + bottomOffset);
        Gizmos.DrawLine(transform.position + rightDir * _detectionRadius + topOffset, transform.position + rightDir * _detectionRadius + bottomOffset);
    }

    private void DrawFanAtHeight(Vector3 heightOffset, Vector3 leftDir, Vector3 rightDir, float halfAngle)
    {
        Vector3 origin = transform.position + heightOffset;

        Gizmos.DrawLine(origin, origin + leftDir * _detectionRadius);
        Gizmos.DrawLine(origin, origin + rightDir * _detectionRadius);

        float angleStep = _detectionAngle / GizmoSegments;
        Vector3 prevPoint = origin + leftDir * _detectionRadius;

        for (int i = 1; i <= GizmoSegments; i++)
        {
            float angle = -halfAngle + angleStep * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            Vector3 point = origin + dir * _detectionRadius;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}
