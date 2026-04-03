using DG.Tweening;
using UnityEngine;

public class SkyLandingEntrance : PatientEntranceBase
{
    [Header("Phase 0: Sky Pause")]
    [Tooltip("침대가 시작하는 높이 (최종 위치 기준 위쪽 미터).")]
    [SerializeField] private float _skyHeight = 10f;

    [Tooltip("하강 시작 전 하늘에서 대기하는 시간 (초). 0이면 즉시 하강.")]
    [SerializeField] private float _pauseBeforeDrop = 0.3f;

    [Header("Phase 1: Fast Descent (하늘 → 호버링 높이)")]
    [Tooltip("1페이즈 하강 시간 (초). 길수록 천천히 내려옴.")]
    [SerializeField] private float _descentDuration = 2.0f;

    [Tooltip("1페이즈 감속 커브. OutQuart=끝에서 확 느려짐, OutCubic=좀 더 균일.")]
    [SerializeField] private Ease _descentEase = Ease.OutQuart;

    [Header("Phase 2: Hover Touchdown (호버링 → 착지)")]
    [Tooltip("호버링 시작 높이 (미터). 이 높이부터 천천히 안착.")]
    [SerializeField] private float _hoverHeight = 1.5f;

    [Tooltip("호버링에서 착지까지 시간 (초). 취이익~ 안착 느낌.")]
    [SerializeField] private float _touchdownDuration = 0.8f;

    [Tooltip("착지 감속 커브.")]
    [SerializeField] private Ease _touchdownEase = Ease.InOutSine;

    [Header("VFX")]
    [Tooltip("로켓 추진 이펙트 (침대 아래). 하강 시작 시 재생, 착지 후 정지.")]
    [SerializeField] private ParticleSystem _thrusterVfx;

    [Tooltip("바닥 이펙트 (먼지/연기). 호버링 높이 도달 시 재생.")]
    [SerializeField] private ParticleSystem _groundVfx;

    private Sequence _sequence;

    public override Sequence Play(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation)
    {
        ForceComplete(bedTransform, finalPosition, finalRotation);

        Vector3 skyPosition = finalPosition + Vector3.up * _skyHeight;
        Vector3 hoverPosition = finalPosition + Vector3.up * _hoverHeight;
        bedTransform.position = skyPosition;
        bedTransform.rotation = finalRotation;

        _sequence = DOTween.Sequence();

        // Phase 0: Dramatic pause in the sky.
        _sequence.AppendInterval(_pauseBeforeDrop);

        // Start thruster VFX.
        _sequence.AppendCallback(() => PlayVfx(_thrusterVfx));

        // Phase 1: Fast descent to hover height.
        _sequence.Append(
            bedTransform.DOMove(hoverPosition, _descentDuration).SetEase(_descentEase));

        // Ground VFX starts at hover height.
        _sequence.AppendCallback(() => PlayVfx(_groundVfx));

        // Phase 2: Slow final touchdown.
        _sequence.Append(
            bedTransform.DOMove(finalPosition, _touchdownDuration).SetEase(_touchdownEase));

        // Stop thruster VFX on touchdown.
        _sequence.AppendCallback(() => StopVfx(_thrusterVfx));

        return _sequence;
    }

    public override void ForceComplete(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation)
    {
        _sequence?.Kill();
        _sequence = null;

        bedTransform.position = finalPosition;
        bedTransform.rotation = finalRotation;

        StopVfx(_thrusterVfx);
        StopVfx(_groundVfx);
    }

    private void PlayVfx(ParticleSystem vfx)
    {
        if (vfx == null)
        {
            return;
        }

        vfx.Play();
    }

    private void StopVfx(ParticleSystem vfx)
    {
        if (vfx == null)
        {
            return;
        }

        vfx.Stop();
    }
}
