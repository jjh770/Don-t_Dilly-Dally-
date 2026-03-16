using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PushableItem : MonoBehaviour, IPushable
{
    public bool IsGrabbed { get; private set; }

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Push(Vector3 direction, float speed)
    {
        IsGrabbed = true;
        Vector3 targetPosition = _rigidbody.position + direction * speed * Time.deltaTime;
        _rigidbody.MovePosition(targetPosition);
    }

    public void Release()
    {
        IsGrabbed = false;
    }
}
