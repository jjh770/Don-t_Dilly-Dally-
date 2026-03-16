using System.Collections;
using UnityEngine;

public class PlayerInteractionAbility : MonoBehaviour
{
    [SerializeField] private float _detectionRadius = 2f;
    [SerializeField] private float _pushSpeed = 3f;
    [SerializeField] private float _throwForce = 5f;
    [SerializeField] private float _throwRotationSpeed = 20f;
    [SerializeField] private float _throwDelay = 0.2f;
    [SerializeField] private float _rotationAngleThreshold = 5f;

    [SerializeField] private Transform _holdPoint;
    [SerializeField] private LayerMask _interactableLayer;

    // 집는 아이템
    private IHoldable _currentHoldable;
    private IHoldable _nearestHoldable;

    private PlayerAnimator _playerAnimator;
    private Camera _camera;
    private bool _isThrowing;

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _camera = Camera.main;
    }

    private void Update()
    {
        FindNearestInteractable();
        HandleInteractInput();
    }

    private void FindNearestInteractable()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRadius, _interactableLayer);

        _nearestHoldable = null;
        float nearestHoldableDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            float distance = Vector3.Distance(transform.position, col.transform.position);

            if (col.TryGetComponent(out HoldableItem holdable) && !holdable.IsHeld)
            {
                if (distance < nearestHoldableDistance)
                {
                    nearestHoldableDistance = distance;
                    _nearestHoldable = holdable;
                }
            }
        }
    }

    private void HandleInteractInput()
    {
        // Holdable: 들고/놓기
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_currentHoldable != null)
            {
                DropItem();
            }
            else if (_nearestHoldable != null)
            {
                PickUpItem(_nearestHoldable);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (_currentHoldable == null) return;
            if (_isThrowing == true) return;

            StartCoroutine(ThrowItemCoroutine());
        }
    }

    private void PickUpItem(IHoldable item)
    {
        _currentHoldable = item;
        _currentHoldable.Hold(_holdPoint);
        _playerAnimator.PlayCarryingAnimation(true);
    }

    private void DropItem()
    {
        _currentHoldable.Drop();
        _currentHoldable = null;
        _playerAnimator.PlayCarryingAnimation(false);
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
        var item = _currentHoldable;
        if (item == null)
        {
            _isThrowing = false;
            yield break;
        }

        _playerAnimator.PlayThrowAnimation();
        yield return new WaitForSeconds(_throwDelay);
        item.Throw(throwDirection, _throwForce);
        _currentHoldable = null;

        // 잡는 애니메이션 취소
        _playerAnimator.ResetThrowAnimation();
        _playerAnimator.PlayCarryingAnimation(false);

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
