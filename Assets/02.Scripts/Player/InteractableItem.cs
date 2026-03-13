using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour, IInteractable
{
    public bool IsHeld { get; private set; }

    private Rigidbody _rigidbody;
    private Collider _collider;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    public void Interact(GameObject interactor)
    {
        IsHeld = true;
        _rigidbody.isKinematic = true;
        _collider.enabled = false;
    }

    public void Drop()
    {
        IsHeld = false;
        _rigidbody.isKinematic = false;
        _collider.enabled = true;
    }
}
