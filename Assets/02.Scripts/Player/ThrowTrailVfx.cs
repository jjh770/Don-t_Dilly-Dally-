using UnityEngine;

public class ThrowTrailVfx : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particleSystems;
    [SerializeField] private float _fadeOutDelay = 2f;

    private bool _isStopScheduled;

    private void Reset()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Awake()
    {
        // Reset이 호출되지 않은 경로(스크립트 AddComponent 등)에서도 안전하게 채웁니다.
        if (_particleSystems == null || _particleSystems.Length == 0)
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }
    }

    public void Play()
    {
        _isStopScheduled = false;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem system = _particleSystems[i];
            if (system == null)
                continue;

            system.Play(true);
        }
    }

    public void Stop()
    {
        if (_isStopScheduled)
            return;

        _isStopScheduled = true;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem system = _particleSystems[i];
            if (system == null)
                continue;

            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // 부모 아이템이 풀로 반환되어도 잔여 파티클이 자연스럽게 페이드아웃 되도록 부모를 분리합니다.
        transform.SetParent(null);
        Destroy(gameObject, _fadeOutDelay);
    }
}
