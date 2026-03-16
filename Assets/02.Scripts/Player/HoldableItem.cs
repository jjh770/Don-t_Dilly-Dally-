using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class HoldableItem : MonoBehaviour, IHoldable
{
    public bool IsHeld { get; private set; }

    [Header("던지기 설정")]
    [SerializeField] private float _upAngle = 0.5f;

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

        Vector3 throwDirection = (direction + Vector3.up * _upAngle).normalized;
        _rigidbody.AddForce(throwDirection * force, ForceMode.Impulse);
    }

    public void Drop()
    {
        IsHeld = false;
        transform.SetParent(null);
        _rigidbody.isKinematic = false;
        _collider.enabled = true;
    }
}
