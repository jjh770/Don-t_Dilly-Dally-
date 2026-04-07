using UnityEngine;

public class AirEventMover : MonoBehaviour
{
    private Vector3 _moveDirection;
    private float _duration;
    private float _speed;
    private float _elapsedTime;

    private float _maxShakeIntensity;
    private float _maxShakeDistance;

    public void Initialize(Vector3 direction, float moveDuration, float moveSpeed, float shakeIntensity, float shakeDistance)
    {
        _moveDirection = direction;
        _duration = moveDuration;
        _speed = moveSpeed;
        _maxShakeIntensity = shakeIntensity;
        _maxShakeDistance = shakeDistance;
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += _moveDirection * _speed * Time.deltaTime;

        ApplyCameraShake();
    }

    private void ApplyCameraShake()
    {
        if (CameraShakeManager.Instance == null) return;

        Vector3 pos = transform.position;
        float distanceToCenter = new Vector2(pos.x, pos.z).magnitude;
        float intensity = Mathf.Lerp(_maxShakeIntensity, 0f, distanceToCenter / _maxShakeDistance);

        CameraShakeManager.Instance.SetIntensity(intensity);
    }
}
