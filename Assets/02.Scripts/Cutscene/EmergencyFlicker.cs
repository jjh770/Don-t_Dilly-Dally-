using UnityEngine;

// 여러 개의 비상등을 동시에 깜빡이게 하는 스크립트.
public class EmergencyFlicker : MonoBehaviour
{
    [SerializeField] private Light[] _emergencyLights;
    [SerializeField] private float _flickerInterval = 0.4f;
    [SerializeField] private float _onIntensity = 5f;
    [SerializeField] private float _offIntensity = 0.5f;

    private float _timer;
    private bool _isOn;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _flickerInterval) return;

        _timer = 0f;
        _isOn = !_isOn;

        float intensity = _isOn ? _onIntensity : _offIntensity;

        // 배열 전체를 한번에 순회.
        for (int i = 0; i < _emergencyLights.Length; i++)
        {
            _emergencyLights[i].intensity = intensity;
        }
    }
}