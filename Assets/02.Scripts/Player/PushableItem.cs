using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PushableItem : MonoBehaviour, IPushable
{
    public bool IsInteracting { get; private set; }
    public Transform Transform => transform;

    private const float GrabDistance = 0.75f;

    private Rigidbody _rigidbody;
    private Transform _player;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!IsInteracting || _player == null) return;

        Vector3 targetPosition = _player.position + _player.forward * GrabDistance;
        targetPosition.y = transform.position.y;

        _rigidbody.MovePosition(targetPosition);
        _rigidbody.MoveRotation(_player.rotation);
    }

    public void Interact(Transform interactor)
    {
        _player = interactor;
        IsInteracting = true;
        _rigidbody.isKinematic = true;
    }

    public void StopInteract()
    {
        _player = null;
        IsInteracting = false;
        _rigidbody.isKinematic = false;
    }
}
