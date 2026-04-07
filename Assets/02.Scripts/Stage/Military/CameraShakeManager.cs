using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [SerializeField] private float shakeFrequency = 25f;

    private Transform _cameraTransform;
    private Vector3 _originalLocalPos;
    private float _currentIntensity;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
            _originalLocalPos = _cameraTransform.localPosition;
        }
    }

    private void LateUpdate()
    {
        if (_cameraTransform == null) return;

        if (_currentIntensity > 0.001f)
        {
            float shakeX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f);
            float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f);

            Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0f) * _currentIntensity;
            _cameraTransform.localPosition = _originalLocalPos + shakeOffset;
        }
        else
        {
            _cameraTransform.localPosition = _originalLocalPos;
        }

        // 매 프레임 리셋 (외부에서 계속 SetIntensity 호출해야 유지됨)
        _currentIntensity = 0f;
    }

    public void SetIntensity(float intensity)
    {
        // 가장 강한 쉐이크 유지
        _currentIntensity = Mathf.Max(_currentIntensity, intensity);
    }

    public void UpdateOriginalPosition()
    {
        if (_cameraTransform != null)
        {
            _originalLocalPos = _cameraTransform.localPosition;
        }
    }
}
