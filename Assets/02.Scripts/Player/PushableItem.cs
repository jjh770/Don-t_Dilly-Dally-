using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PushableItem : MonoBehaviour
{
    private const float GrabDistance = 0.75f;

    private Rigidbody _rigidbody;
    private Transform _localInteractor;
    private bool _isInteractionLocked;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (_localInteractor == null)
        {
            return;
        }

        Vector3 targetPosition = _localInteractor.position + _localInteractor.forward * GrabDistance;
        targetPosition.y = transform.position.y;

        _rigidbody.MovePosition(targetPosition);
        _rigidbody.MoveRotation(_localInteractor.rotation);
    }

    public void BeginLocalPush(Transform interactor)
    {
        if (interactor == null)
        {
            return;
        }

        _localInteractor = interactor;
        UpdateKinematicState();
    }

    public void EndLocalPush()
    {
        _localInteractor = null;
        UpdateKinematicState();
    }

    public void SetInteractionLocked(bool isLocked)
    {
        _isInteractionLocked = isLocked;
        UpdateKinematicState();
    }

    private void UpdateKinematicState()
    {
        _rigidbody.isKinematic = _isInteractionLocked || _localInteractor != null;
    }
}
