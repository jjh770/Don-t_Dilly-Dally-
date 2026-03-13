using UnityEngine;

public class PlayerInteractionAbility : MonoBehaviour
{
    [SerializeField] private float _detectionRadius = 2f;
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private LayerMask _interactableLayer;

    private InteractableItem _currentItem;
    private InteractableItem _nearestItem;
    private PlayerAnimator _playerAnimator;

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void Update()
    {
        FindNearestInteractable();
        HandleInteractInput();
    }

    private void FindNearestInteractable()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRadius, _interactableLayer);

        _nearestItem = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            if (col.TryGetComponent(out InteractableItem item) && !item.IsHeld)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _nearestItem = item;
                }
            }
        }
    }

    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_currentItem != null)
            {
                DropItem();
            }
            else if (_nearestItem != null)
            {
                PickUpItem(_nearestItem);
            }
        }
    }

    private void PickUpItem(InteractableItem item)
    {
        _currentItem = item;
        _currentItem.Interact(gameObject);
        _currentItem.transform.SetParent(_holdPoint);
        _currentItem.transform.localPosition = Vector3.zero;
        _currentItem.transform.localRotation = Quaternion.identity;
        _playerAnimator.SetCarrying(true);
    }

    private void DropItem()
    {
        _currentItem.Drop();
        _currentItem.transform.SetParent(null);
        _currentItem = null;
        _playerAnimator.SetCarrying(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}
