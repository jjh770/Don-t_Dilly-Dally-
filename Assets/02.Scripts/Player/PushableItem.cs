using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PushableItem : MonoBehaviour, IPushable
{
    public bool IsInteracting { get; private set; }
    public Transform Transform => transform;

    private Rigidbody _rigidbody;
    private Transform _player;
    private float _grabDistance;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsInteracting || _player == null) return;

        // 플레이어 앞에 고정
        Vector3 targetPosition = _player.position + _player.forward * _grabDistance;
        targetPosition.y = transform.position.y;
        transform.position = targetPosition;
        transform.rotation = _player.rotation;
    }

    public void Interact()
    {
        IsInteracting = true;
        _rigidbody.isKinematic = true;
    }

    public void StopInteract()
    {
        IsInteracting = false;
        _rigidbody.isKinematic = false;
        _player = null;
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _grabDistance = Vector3.Distance(player.position, transform.position);
    }

    public void Push(Vector3 direction, float speed)
    {
        // Update에서 처리
    }
}
