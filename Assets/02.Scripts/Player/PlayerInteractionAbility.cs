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

    [Header("밀고 당기기 설정")]
    [SerializeField] private float _pushSpeedMultiplier = 0.5f;
    [SerializeField] private float _pushRotationMultiplier = 0.2f;

    [Header("던지기 설정")]
    [SerializeField] private float _throwForce = 5f;
    [SerializeField] private float _throwRotationSpeed = 20f;
    [SerializeField] private float _throwDelay = 0.2f;
    [SerializeField] private float _rotationAngleThreshold = 5f;

    [SerializeField] private Transform _holdPoint;
    [SerializeField] private LayerMask _interactableLayer;

    private IInteractable _currentInteractable;
    private IInteractable _nearestInteractable;

    private PlayerAnimator _playerAnimator;
    private PlayerMovementAbility _playerMovement;
    private Camera _camera;
    private bool _isThrowing;
    private float _detectionAngleCos;

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _playerMovement = GetComponent<PlayerMovementAbility>();
        _camera = Camera.main;
        _detectionAngleCos = Mathf.Cos(_detectionAngle * HalfAngleMultiplier * Mathf.Deg2Rad);
    }

    private void Update()
    {
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
            if (col.TryGetComponent(out IInteractable interactable) && !interactable.IsInteracting)
            {
                // 높이 체크
                float detectionCenterY = transform.position.y + _detectionHeightOffset;
                float heightDiff = Mathf.Abs(col.transform.position.y - detectionCenterY);
                if (heightDiff > _detectionHeight) continue;

                // 시야각 체크
                Vector3 directionToItem = col.transform.position - transform.position;
                directionToItem.y = 0;
                float dot = Vector3.Dot(transform.forward, directionToItem.normalized);

                if (dot < _detectionAngleCos) continue;

                float sqrDistance = (col.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    _nearestInteractable = interactable;
                }
            }
        }
    }

    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
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
        _currentInteractable = interactable;

        // 아이템 집기
        if (interactable is IHoldable holdable)
        {
            holdable.Hold(_holdPoint);
            _playerAnimator.PlayHoldAnimation(true);
        }
        // 아이템 밀기
        else if (interactable is IPushable pushable)
        {
            pushable.Interact();
            ((PushableItem)pushable).SetPlayer(transform);
            _playerAnimator.PlayGrabAnimation(true);
            _playerMovement.SetSpeedMultiplier(_pushSpeedMultiplier, _pushRotationMultiplier);
        }
    }

    private void StopInteract()
    {
        // 아이템 놓기
        if (_currentInteractable is IHoldable holdable)
        {
            holdable.Drop();
            _playerAnimator.PlayHoldAnimation(false);
        }
        // 아이템 손에서 떼기
        else if (_currentInteractable is IPushable)
        {
            _currentInteractable.StopInteract();
            _playerAnimator.PlayGrabAnimation(false);
            _playerAnimator.PlayPushAnimation(false);
            _playerMovement.SetSpeedMultiplier(DefaultSpeedMultiplier, DefaultSpeedMultiplier);
        }

        _currentInteractable = null;
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
        holdable.Throw(throwDirection, _throwForce);
        _currentInteractable = null;

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
