using UnityEngine;

public class IndicatorBillboard : MonoBehaviour
{
    [SerializeField] private bool _lockX;
    [SerializeField] private bool _lockY;
    [SerializeField] private bool _lockZ;

    private Camera _mainCamera;
    private Transform _target;
    private Vector3 _offset;

    public void Initialize(Transform target, Vector3 offset)
    {
        _target = target;
        _offset = offset;
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_mainCamera == null)
        {
            return;
        }

        UpdatePosition();
        UpdateRotation();
    }

    private void UpdatePosition()
    {
        if (_target == null)
        {
            return;
        }

        transform.position = _target.position + _offset;
    }

    private void UpdateRotation()
    {
        Vector3 direction = _mainCamera.transform.position - transform.position;

        if (_lockX) direction.x = 0f;
        if (_lockY) direction.y = 0f;
        if (_lockZ) direction.z = 0f;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-direction);
        }
    }
}
