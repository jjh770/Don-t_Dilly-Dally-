using DontDillyDally.Data;
using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHeldItemController : MonoBehaviour, IHeldItemInteractor
{
    [Header("들기 설정")]
    [SerializeField] private Transform _holdPoint;

    [Header("던지기 설정")]
    [SerializeField] private float _throwForce = 5f;
    [SerializeField] private float _throwRotationSpeed = 20f;
    [SerializeField] private float _throwDelay = 0.2f;
    [SerializeField] private float _rotationAngleThreshold = 5f;

    private IInteractable _currentHeldInteractable;
    private ItemObject _currentHeldItem;

    private IInteractable _pendingHoldInteractable;
    private ItemObject _pendingHeldItem;
    private NetworkItemOwnership _pendingOwnership;
    private Action _onPendingHoldFailed;
    private Func<bool> _onPendingHoldBeforeHold;
    private bool _isExternalInteractionLocked;
    private bool _isThrowing;

    private PlayerAnimator _playerAnimator;
    private PlayerMovementAbility _playerMovement;
    private Camera _camera;
    private Collider[] _playerColliders;
    private readonly object _movementLockSource = new();

    public ItemObject CurrentHeldItem => _currentHeldItem;
    public IInteractable CurrentHeldInteractable => _currentHeldInteractable;
    public bool IsHoldingHoldable => _currentHeldInteractable is IHoldable;
    public bool IsExternalInteractionLocked => _isExternalInteractionLocked;
    public bool IsThrowing => _isThrowing;
    public Transform HoldPoint => _holdPoint;

    public PhotonView GetInteractorPhotonView() => GetComponent<PhotonView>();
    public Transform GetHandAttachPoint() => _holdPoint;

    public event Action<ItemObject> HeldItemChanged;

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _playerMovement = GetComponent<PlayerMovementAbility>();
        _camera = Camera.main;
        _playerColliders = GetComponentsInChildren<Collider>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isThrowing = false;
        ClearPendingHold();
        EndHeldItemInteractionLock();
    }

    public bool TryPickupInteractable(IInteractable interactable, Action onFailed = null, Func<bool> onBeforeHold = null)
    {
        if (!TryResolveInteractableComponent(interactable, out _))
        {
            return false;
        }

        if (_currentHeldInteractable != null || _currentHeldItem != null)
        {
            return false;
        }

        if (interactable is not IHoldable)
        {
            return false;
        }

        ClearPendingHold();

        _onPendingHoldFailed = onFailed;
        _onPendingHoldBeforeHold = onBeforeHold;
        return TryStartHold(interactable);
    }

    public bool TryReleaseHeldItem(ItemObject expectedItem = null)
    {
        if (_isThrowing)
        {
            return false;
        }

        return TryReleaseHeldItemInternal(expectedItem, out _);
    }

    public bool TryConsumeHeldItem(ItemObject expectedItem = null)
    {
        if (_isThrowing)
        {
            return false;
        }

        if (!TryReleaseHeldItemInternal(expectedItem, out ItemObject releasedItem))
        {
            return false;
        }

        if (releasedItem == null)
        {
            return false;
        }

        return ItemRecycleUtility.TryRecycle(releasedItem);
    }

    public bool TryBeginHeldItemInteractionLock(ItemObject expectedHeldItem)
    {
        if (_isExternalInteractionLocked)
        {
            return false;
        }

        if (_currentHeldItem == null || _currentHeldItem != expectedHeldItem)
        {
            return false;
        }

        if (_currentHeldInteractable is not IHoldable)
        {
            return false;
        }

        _isExternalInteractionLocked = true;
        _playerMovement?.SetMovementLocked(_movementLockSource, true);
        return true;
    }

    public void EndHeldItemInteractionLock()
    {
        if (!_isExternalInteractionLocked)
        {
            return;
        }

        _isExternalInteractionLocked = false;
        _playerMovement?.SetMovementLocked(_movementLockSource, false);
    }

    public bool TryBeginThrow()
    {
        if (_isThrowing)
        {
            return false;
        }

        if (_currentHeldInteractable is not IHoldable)
        {
            return false;
        }

        StartCoroutine(ThrowSequence());
        SoundManager.Instance.Play(SFXKey.PlayerThrow, SoundType.Local);
        return true;
    }

    private IEnumerator ThrowSequence()
    {
        _isThrowing = true;

        Vector3 throwDirection = GetMouseWorldDirection();
        Quaternion targetRotation = Quaternion.LookRotation(throwDirection);

        while (Quaternion.Angle(transform.rotation, targetRotation) > _rotationAngleThreshold)
        {
            if (_currentHeldInteractable is not IHoldable)
            {
                _isThrowing = false;
                yield break;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _throwRotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;

        if (_currentHeldInteractable is not IHoldable holdable)
        {
            _isThrowing = false;
            yield break;
        }

        _playerAnimator?.PlayThrowAnimation();
        yield return new WaitForSeconds(_throwDelay);

        if (_currentHeldInteractable is not IHoldable holdableAfterDelay)
        {
            _playerAnimator?.ResetThrowAnimation();
            _playerAnimator?.PlayHoldAnimation(false);
            _isThrowing = false;
            yield break;
        }

        holdableAfterDelay.Throw(throwDirection, _throwForce, _playerColliders);
        _currentHeldInteractable = null;
        SetCurrentHeldItem(null);

        _playerAnimator?.ResetThrowAnimation();
        _playerAnimator?.PlayHoldAnimation(false);

        _isThrowing = false;
    }

    private Vector3 GetMouseWorldDirection()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            return (hitPoint - transform.position).normalized;
        }

        return transform.forward;
    }

    private bool TryStartHold(IInteractable interactable)
    {
        if (interactable is not IHoldable)
        {
            return false;
        }

        if (HoldPoint == null)
        {
            return false;
        }

        if (!TryResolveHeldItem(interactable, out ItemObject itemObject))
        {
            return false;
        }

        NetworkItemOwnership ownership = itemObject.NetworkOwnership;
        if (ownership == null)
        {
            return false;
        }

        _pendingHoldInteractable = interactable;
        _pendingHeldItem = itemObject;
        _pendingOwnership = ownership;

        ownership.RequestOwnershipWithCallback(
            onAcquired: () =>
            {
                // 소유권 콜백은 늦게 도착할 수 있으므로, 아직도 같은 아이템을 정상적으로 집을 수 있는지 다시 확인합니다.
                if (!TryGetPendingHoldContext(out IInteractable pendingInteractable, out IHoldable pendingHoldable, out ItemObject pendingHeldItem))
                {
                    FailPendingHold();
                    return;
                }

                Func<bool> beforeHold = _onPendingHoldBeforeHold;
                if (beforeHold != null && !beforeHold())
                {
                    FailPendingHold();
                    return;
                }

                BeginHold(pendingInteractable, pendingHoldable, pendingHeldItem);
            },
            onFailed: () =>
            {
                FailPendingHold();
            }
        );

        return ownership.IsOwnedLocally;
    }

    private void BeginHold(IInteractable interactable, IHoldable holdable, ItemObject itemObject)
    {
        int actorNumber = PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : -1;

        holdable.Interact(HoldPoint, actorNumber);
        itemObject.NetworkOwnership?.NotifyHoldStarted();
        itemObject.NotifyLeftSource();

        _currentHeldInteractable = interactable;
        SetCurrentHeldItem(itemObject);

        ClearPendingHold();

        _playerAnimator?.PlayHoldAnimation(true);
        SoundManager.Instance.Play(SFXKey.PlayerPickUp, SoundType.Local);

    }

    private bool TryReleaseHeldItemInternal(ItemObject expectedItem, out ItemObject releasedItem)
    {
        releasedItem = null;

        if (_currentHeldItem == null)
        {
            return false;
        }

        if (expectedItem != null && _currentHeldItem != expectedItem)
        {
            return false;
        }

        if (_currentHeldInteractable is not IHoldable holdable)
        {
            return false;
        }

        releasedItem = _currentHeldItem;
        holdable.StopInteract();

        _playerAnimator?.PlayHoldAnimation(false);
        _currentHeldInteractable = null;
        SetCurrentHeldItem(null);
        SoundManager.Instance.Play(SFXKey.PlayerDrop, SoundType.Local);
        return true;
    }

    private bool TryResolveHeldItem(IInteractable interactable, out ItemObject itemObject)
    {
        itemObject = null;

        if (interactable is not Component component)
        {
            return false;
        }

        if (component == null)
        {
            return false;
        }

        itemObject = component.GetComponent<ItemObject>();
        return itemObject != null;
    }

    private bool TryGetPendingHoldContext(out IInteractable interactable, out IHoldable holdable, out ItemObject itemObject)
    {
        interactable = null;
        holdable = null;
        itemObject = null;

        if (_pendingOwnership == null || !_pendingOwnership.IsOwnedLocally)
        {
            return false;
        }

        if (!TryResolveInteractableComponent(_pendingHoldInteractable, out Component interactableComponent))
        {
            return false;
        }

        if (!interactableComponent.TryGetComponent(out holdable))
        {
            return false;
        }

        if (_pendingHeldItem == null)
        {
            return false;
        }

        interactable = _pendingHoldInteractable;
        itemObject = _pendingHeldItem;
        return true;
    }

    private void FailPendingHold()
    {
        // 픽업이 실패하면 "이미 집은 것처럼" 남지 않도록 보류 상태와 실패 콜백을 함께 정리합니다.
        Action failedCallback = _onPendingHoldFailed;
        ClearPendingHold();
        failedCallback?.Invoke();
    }

    private static bool TryResolveInteractableComponent(IInteractable interactable, out Component component)
    {
        component = interactable as Component;
        return component != null;
    }

    private void ClearPendingHold()
    {
        _pendingOwnership?.CancelPendingRequest();
        _pendingHoldInteractable = null;
        _pendingHeldItem = null;
        _pendingOwnership = null;
        _onPendingHoldFailed = null;
        _onPendingHoldBeforeHold = null;
    }

    private void SetCurrentHeldItem(ItemObject newItem)
    {
        if (_currentHeldItem == newItem)
        {
            return;
        }

        _currentHeldItem = newItem;
        HeldItemChanged?.Invoke(_currentHeldItem);
    }
}
