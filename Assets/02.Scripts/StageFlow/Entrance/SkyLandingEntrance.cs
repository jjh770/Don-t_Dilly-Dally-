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

    [Header("VFX: Thruster (로켓 분화)")]
    [Tooltip("로켓 분화 파티클 배열 (바퀴 4개). 침대 자식으로 배치, Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem[] _thrusterFx;

    [Tooltip("로켓 분화 시작 시점 (초). 하강 시작에 맞추세요.")]
    [SerializeField] private float _thrusterStartTime = 0.3f;

    [Tooltip("로켓 분화 정지 시점 (초). 착지 완료에 맞추세요.")]
    [SerializeField] private float _thrusterStopTime = 3.1f;

    [Header("VFX: Ground Smoke (바닥 연기)")]
    [Tooltip("바닥 연기 파티클. 로켓이 바닥에 가까워지면 바닥에서 연기가 퍼짐. 착륙 지점에 배치.")]
    [SerializeField] private ParticleSystem _groundSmokeFx;

    [Tooltip("바닥 연기 시작 시점 (초). 호버링 높이 근처에 맞추세요.")]
    [SerializeField] private float _groundSmokeStartTime = 2.3f;

    [Tooltip("바닥 연기 정지 시점 (초). 새 파티클 발생만 멈추고 기존 연기는 서서히 사라짐.")]
    [SerializeField] private float _groundSmokeStopTime = 3.5f;

    [Header("VFX: Landing Smoke (착지 자욱한 연기)")]
    [Tooltip("착지 연기 루트 오브젝트. 자식에 있는 모든 ParticleSystem을 자동으로 재생. 착륙 지점에 배치.")]
    [SerializeField] private GameObject _landingSmokeRoot;

    [Tooltip("착지 연기 시작 시점 (초). 바닥에 닿는 순간에 맞추세요.")]
    [SerializeField] private float _landingSmokeStartTime = 3.1f;

    private Sequence _sequence;
    private AudioSource _rocketHoverLoopSource;

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

        // Phase 1: Fast descent to hover height.
        _sequence.Append(
            bedTransform.DOMove(hoverPosition, _descentDuration).SetEase(_descentEase));

        // Phase 2: Slow final touchdown.
        _sequence.Append(
            bedTransform.DOMove(finalPosition, _touchdownDuration).SetEase(_touchdownEase));

        // VFX callbacks at user-specified times.
        _sequence.InsertCallback(_thrusterStartTime, PlayThrusters);
        _sequence.InsertCallback(_thrusterStopTime, StopThrusters);
        _sequence.InsertCallback(_groundSmokeStartTime, () => PlayFx(_groundSmokeFx));
        _sequence.InsertCallback(_groundSmokeStopTime, () => StopFx(_groundSmokeFx));
        _sequence.InsertCallback(_landingSmokeStartTime, () => PlayAllFx(_landingSmokeRoot));

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

        StopRocketHoverLoop();
        ClearThrusters();
        ClearFx(_groundSmokeFx);
        ClearAllFx(_landingSmokeRoot);
    }

    private void PlayThrusters()
    {
        if (_thrusterFx == null) return;
        foreach (ParticleSystem fx in _thrusterFx) PlayFx(fx);
        StopRocketHoverLoop();
        _rocketHoverLoopSource = SoundManager.Instance.PlayLoop(SFXKey.PatinetRocketHovering);
    }

    private void StopThrusters()
    {
        if (_thrusterFx != null)
        {
            foreach (ParticleSystem fx in _thrusterFx) StopFx(fx);
        }

        StopRocketHoverLoop();
        SoundManager.Instance.Play(SFXKey.PatientRocketLanding, SoundType.Local);
    }

    private void ClearThrusters()
    {
        if (_thrusterFx == null) return;
        foreach (ParticleSystem fx in _thrusterFx) ClearFx(fx);
    }

    private void StopRocketHoverLoop()
    {
        SoundManager.Instance.StopSFX(_rocketHoverLoopSource);
        _rocketHoverLoopSource = null;
    }
}
