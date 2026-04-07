using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [SerializeField] private float shakeFrequency = 25f;

    private Transform cameraTransform;
    private Vector3 originalLocalPos;
    private float currentIntensity;

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
            cameraTransform = Camera.main.transform;
            originalLocalPos = cameraTransform.localPosition;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        if (currentIntensity > 0.001f)
        {
            float shakeX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f);
            float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f);

            Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0f) * currentIntensity;
            cameraTransform.localPosition = originalLocalPos + shakeOffset;
        }
        else
        {
            cameraTransform.localPosition = originalLocalPos;
        }

        // 매 프레임 리셋 (외부에서 계속 SetIntensity 호출해야 유지됨)
        currentIntensity = 0f;
    }

    public void SetIntensity(float intensity)
    {
        // 가장 강한 쉐이크 유지
        currentIntensity = Mathf.Max(currentIntensity, intensity);
    }

    public void UpdateOriginalPosition()
    {
        if (cameraTransform != null)
        {
            originalLocalPos = cameraTransform.localPosition;
        }
    }
}
