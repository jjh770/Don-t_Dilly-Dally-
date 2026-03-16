using System.Collections;
using UnityEngine;

public class PlayerInteractionAbility : MonoBehaviour
{
    [Header("아이템 탐지 설정")]
    [SerializeField] private float _detectionRadius = 2f;

    [Header("밀고 당기기 설정")]
    [SerializeField] private float _pushSpeedMultiplier = 0.5f;

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

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _playerMovement = GetComponent<PlayerMovementAbility>();
        _camera = Camera.main;
    }

    private void Update()
    {
        FindNearestInteractable();
        HandleInteractInput();
        HandlePushableMovement();
    }

    private void FindNearestInteractable()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRadius, _interactableLayer);

        _nearestInteractable = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            if (col.TryGetComponent(out IInteractable interactable) && !interactable.IsInteracting)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _nearestInteractable = interactable;
                }
            }
        }
    }

    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 손에 아이템이 있으면 동작 그만 두기
            if (_currentInteractable != null)
            {
                StopInteract();
            }
            // 근처에 아이템이 있으면 동작하기
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
            _playerMovement.SetSpeedMultiplier(_pushSpeedMultiplier);
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
            _playerMovement.SetSpeedMultiplier(1f);
        }

        _currentInteractable = null;
    }

    private void HandlePushableMovement()
    {
        if (_currentInteractable is not IPushable) return;

        Vector3 moveDirection = _playerMovement.MoveDirection;
        bool isMoving = moveDirection.sqrMagnitude > 0.01f;
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
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}
