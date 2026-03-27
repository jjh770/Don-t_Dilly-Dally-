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
    private bool _isExternalInteractionLocked;
    private bool _isThrowing;

    private PlayerAnimator _playerAnimator;
    private PlayerMovementAbility _playerMovement;
    private Camera _camera;
    private Collider[] _playerColliders;

    public ItemObject CurrentHeldItem => _currentHeldItem;
    public IInteractable CurrentHeldInteractable => _currentHeldInteractable;
    public bool IsHoldingHoldable => _currentHeldInteractable is IHoldable;
    public bool IsExternalInteractionLocked => _isExternalInteractionLocked;
    public bool IsThrowing => _isThrowing;
    public Transform HoldPoint => _holdPoint;

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
        ClearPendingHold();
        EndHeldItemInteractionLock();
    }

    public bool TryPickupInteractable(IInteractable interactable, Action onFailed = null)
    {
        if (interactable == null)
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

        _onPendingHoldFailed = onFailed;
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

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.Destroy(releasedItem.gameObject);
        }
        else
        {
            Destroy(releasedItem.gameObject);
        }

        return true;
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
        _playerMovement?.SetMovementLocked(true);
        return true;
    }

    public void EndHeldItemInteractionLock()
    {
        if (!_isExternalInteractionLocked)
        {
            return;
        }

        _isExternalInteractionLocked = false;
        _playerMovement?.SetMovementLocked(false);
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
        return true;
    }

    private IEnumerator ThrowSequence()
    {
        _isThrowing = true;

        Vector3 throwDirection = GetMouseWorldDirection();
        Quaternion targetRotation = Quaternion.LookRotation(throwDirection);

        while (Quaternion.Angle(transform.rotation, targetRotation) > _rotationAngleThreshold)
        {
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

        holdable.Throw(throwDirection, _throwForce, _playerColliders);
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

        ClearPendingHold();

        _pendingHoldInteractable = interactable;
        _pendingHeldItem = itemObject;
        _pendingOwnership = ownership;

        ownership.RequestOwnershipWithCallback(
            onAcquired: () =>
            {
                if (_pendingHoldInteractable is IHoldable pendingHoldable && _pendingHeldItem != null)
                {
                    BeginHold(_pendingHoldInteractable, pendingHoldable, _pendingHeldItem);
                }
            },
            onFailed: () =>
            {
                Action failedCallback = _onPendingHoldFailed;
                ClearPendingHold();
                failedCallback?.Invoke();
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
        return true;
    }

    private bool TryResolveHeldItem(IInteractable interactable, out ItemObject itemObject)
    {
        itemObject = null;

        if (interactable is not Component component)
        {
            return false;
        }

        itemObject = component.GetComponent<ItemObject>();
        return itemObject != null;
    }

    private void ClearPendingHold()
    {
        _pendingOwnership?.CancelPendingRequest();
        _pendingHoldInteractable = null;
        _pendingHeldItem = null;
        _pendingOwnership = null;
        _onPendingHoldFailed = null;
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
