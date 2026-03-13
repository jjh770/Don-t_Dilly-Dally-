using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class HoldableItem : MonoBehaviour, IHoldable
{
    public bool IsHeld { get; private set; }

    private Rigidbody _rigidbody;
    private Collider _collider;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    public void Hold(Transform holdPoint)
    {
        IsHeld = true;
        _rigidbody.isKinematic = true;
        _collider.enabled = false;
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Throw(Vector3 direction, float force)
    {
        IsHeld = false;
        transform.SetParent(null);
        _rigidbody.isKinematic = false;
        _collider.enabled = true;
        _rigidbody.AddForce(direction * force, ForceMode.Impulse);
    }

    public void Drop()
    {
        IsHeld = false;
        transform.SetParent(null);
        _rigidbody.isKinematic = false;
        _collider.enabled = true;
    }
}
