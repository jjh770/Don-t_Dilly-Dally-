using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerWalkDustPresenter : MonoBehaviour
{
    [Header("발먼지 VFX")]
    [Tooltip("걸음 시작 순간 팍 터지는 버스트 파티클")]
    [SerializeField] private ParticleSystem _burstParticles;

    [Tooltip("걷는 동안 굴뚝처럼 지속되는 루프 파티클")]
    [SerializeField] private ParticleSystem _loopParticles;

    [Tooltip("걸음 시작 시 한 번에 방출할 버스트 파티클 개수")]
    [SerializeField] private int _burstCount = 15;

    [Tooltip("버스트 재방출 쿨다운 (초). 짧게 멈췄다 다시 걸을 땐 동일한 걸음의 연속으로 간주해 중복 버스트를 막습니다.")]
    [SerializeField, Min(0f)] private float _burstCooldown = 0.5f;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private Animator _animator;
    private bool _wasWalking;
    private float _lastBurstTime = float.NegativeInfinity;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_burstParticles == null || _loopParticles == null)
        {
            Debug.LogError($"[{nameof(PlayerWalkDustPresenter)}] 파티클 참조 누락. 인스펙터에서 _burstParticles, _loopParticles를 할당하세요.", this);
        }
    }

    private void OnDisable()
    {
        StopDustImmediate();
    }

    private void Update()
    {
        bool isWalking = _animator.GetBool(IsWalkingHash);
        if (isWalking == _wasWalking)
        {
            return;
        }

        _wasWalking = isWalking;
        if (isWalking)
        {
            PlayDust();
        }
        else
        {
            StopDust();
        }
    }

    private void PlayDust()
    {
        if (_burstParticles != null && Time.time - _lastBurstTime >= _burstCooldown)
        {
            _burstParticles.Emit(_burstCount);
            _lastBurstTime = Time.time;
        }

        if (_loopParticles != null)
        {
            _loopParticles.Play(false);
        }
    }

    private void StopDust()
    {
        if (_loopParticles != null)
        {
            _loopParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void StopDustImmediate()
    {
        if (_loopParticles != null)
        {
            _loopParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
