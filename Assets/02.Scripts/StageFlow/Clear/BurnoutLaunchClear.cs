using DG.Tweening;
using UnityEngine;

// 수술 성공 클리어 연출 1: 드래그 레이싱 번아웃.
// 뒷바퀴가 번아웃하며 연기가 나고, 앞이 들리면서 드래그 레이싱처럼 튀어나간다.
public class BurnoutLaunchClear : PatientClearBase
{
    [Header("Phase 0: Burnout (뒷바퀴 공회전)")]
    [Tooltip("번아웃 떨림 시간 (초). 뒷바퀴가 헛돌며 침대가 부들부들.")]
    [SerializeField] private float _burnoutDuration = 1.5f;

    [Tooltip("번아웃 중 떨림 강도.")]
    [SerializeField] private float _burnoutShakeStrength = 0.08f;

    [Tooltip("번아웃 중 떨림 진동수.")]
    [SerializeField] private int _burnoutVibrato = 30;

    [Header("Phase 1: Wheelie Tilt (앞바퀴 들림)")]
    [Tooltip("앞바퀴가 들리기 시작하는 시점 (초).")]
    [SerializeField] private float _wheelieStartTime = 0.8f;

    [Tooltip("앞바퀴 들림 각도 (도). 침대 Y90 기준, 음수 = 앞이 들림.")]
    [SerializeField] private Vector3 _wheelihTiltAngle = new(-15f, 0f, 0f);

    [Tooltip("앞바퀴 들림 시간 (초).")]
    [SerializeField] private float _wheelieTiltDuration = 0.4f;

    [Tooltip("앞바퀴 들릴 때 Y축 보정 높이.")]
    [SerializeField] private float _wheelieLiftHeight = 0.2f;

    [Header("Phase 2: Launch (발사)")]
    [Tooltip("발사 시작 시점 (초). 번아웃 끝 + 약간의 딜레이.")]
    [SerializeField] private float _launchStartTime = 1.5f;

    [Tooltip("발사 방향 오프셋 (최종 위치 기준). X+ = 침대 앞 방향.")]
    [SerializeField] private Vector3 _launchOffset = new(12f, 0f, 0f);

    [Tooltip("발사 이동 시간 (초). 짧을수록 빠르게 튀어나감.")]
    [SerializeField] private float _launchDuration = 0.6f;

    [Tooltip("발사 중 기울기 유지 각도. 음수 = 앞이 더 들림.")]
    [SerializeField] private Vector3 _launchTiltAngle = new(-20f, 0f, 0f);

    [Tooltip("발사 시 추가 기울기 전환 시간 (초).")]
    [SerializeField] private float _launchTiltDuration = 0.15f;

    [Header("VFX: Burnout Smoke (뒷바퀴 연기)")]
    [Tooltip("뒷바퀴 연기 파티클 배열. 침대 뒷바퀴 위치에 배치.")]
    [SerializeField] private ParticleSystem[] _burnoutSmokeFx;

    [Tooltip("번아웃 연기 시작 시점 (초).")]
    [SerializeField] private float _burnoutSmokeStartTime = 0f;

    [Tooltip("번아웃 연기 정지 시점 (초). 발사 후에도 잠시 남게.")]
    [SerializeField] private float _burnoutSmokeStopTime = 2.0f;

    [Header("VFX: Launch Trail (발사 잔상 연기)")]
    [Tooltip("발사 후 남는 연기 트레일. 침대 자식으로 배치하면 같이 이동.")]
    [SerializeField] private ParticleSystem _launchTrailFx;

    [Tooltip("발사 트레일 시작 시점 (초).")]
    [SerializeField] private float _launchTrailStartTime = 1.5f;

    [Header("Door Settings")]
    [SerializeField] private Transform _doorParent;
    [SerializeField] private float _doorOpenTime = 1.3f;
    [SerializeField] private float _doorOpenAngle = 90f;
    [SerializeField] private float _doorSlamDuration = 0.1f;
    [SerializeField] private float _doorStayOpenDuration = 0.8f;
    [SerializeField] private float _doorSwingDuration = 2.0f;

    private Sequence _sequence;
    private bool _hasCachedState;
    private Vector3 _cachedRootPosition;
    private Vector3 _cachedBedPosition;
    private Quaternion _cachedBedRotation;

    public override Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        // ForceComplete 먼저 — 이전 연출 중간 상태를 정리한 뒤 캐싱.
        // (_hasCachedState 가드로 첫 호출 시 zero 원복 방지.)
        ForceComplete(patientRoot, bedTransform, patientTransform);

        _cachedRootPosition = patientRoot.position;
        _cachedBedPosition = bedTransform.position;
        _cachedBedRotation = bedTransform.rotation;
        _hasCachedState = true;

        Vector3 startPosition = _cachedBedPosition;
        Quaternion startRotation = _cachedBedRotation;

        SoundManager.Instance.Play(SFXKey.PatientSurgeryComplete, SoundType.Local);

        _sequence = DOTween.Sequence();

        // Phase 0: 번아웃 — 침대가 부들부들 떨림.
        _sequence.Append(
            patientRoot.DOShakePosition(_burnoutDuration, _burnoutShakeStrength,
                vibrato: _burnoutVibrato, fadeOut: true));

        // 번아웃 연기.
        _sequence.InsertCallback(_burnoutSmokeStartTime, PlayBurnoutSmoke);
        _sequence.InsertCallback(_burnoutSmokeStopTime, StopBurnoutSmoke);

        // Phase 1: 앞바퀴 들림 (wheelie).
        Quaternion wheelieRotation = startRotation * Quaternion.Euler(_wheelihTiltAngle);

        _sequence.Insert(_wheelieStartTime,
            bedTransform.DORotateQuaternion(wheelieRotation, _wheelieTiltDuration)
                .SetEase(Ease.OutQuad));

        _sequence.Insert(_wheelieStartTime,
            bedTransform.DOMoveY(startPosition.y + _wheelieLiftHeight, _wheelieTiltDuration)
                .SetEase(Ease.OutQuad));

        // Phase 2: 발사! 드래그 레이싱처럼 튀어나감.
        Vector3 launchTarget = startPosition + _launchOffset;
        Quaternion launchRotation = startRotation * Quaternion.Euler(_launchTiltAngle);

        _sequence.InsertCallback(_launchStartTime,
            () => SoundManager.Instance.Play(SFXKey.PatientBurnout, SoundType.Local));

        _sequence.Insert(_launchStartTime,
            bedTransform.DORotateQuaternion(launchRotation, _launchTiltDuration)
                .SetEase(Ease.InQuad));

        _sequence.Insert(_launchStartTime,
            bedTransform.DOMove(launchTarget, _launchDuration)
                .SetEase(Ease.InQuart));

        // 발사 트레일 연기.
        _sequence.InsertCallback(_launchTrailStartTime, () => PlayFx(_launchTrailFx));

        // 시퀀스 끝나면 파티클 정리만. 위치 원복은 EntranceDirector가 담당.
        _sequence.OnComplete(() =>
        {
            ClearBurnoutSmoke();
            ClearFx(_launchTrailFx);
        });

        // 문 연출.
        Sequence doorSequence = CreateDoorSequence(
            _doorParent, _doorOpenAngle, _doorSlamDuration,
            _doorStayOpenDuration, _doorSwingDuration);
        _sequence.Insert(_doorOpenTime, doorSequence);

        return _sequence;
    }

    public override void ForceComplete(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        _sequence?.Kill();
        _sequence = null;

        patientRoot.DOKill();
        bedTransform.DOKill();

        // DOShakePosition/DOMove 중간에 Kill되면 흔들린 위치에서 멈추므로 원복.
        if (_hasCachedState)
        {
            patientRoot.position = _cachedRootPosition;
            bedTransform.position = _cachedBedPosition;
            bedTransform.rotation = _cachedBedRotation;
        }

        ClearBurnoutSmoke();
        ClearFx(_launchTrailFx);
        ResetDoor(_doorParent);
    }

    private void PlayBurnoutSmoke()
    {
        if (_burnoutSmokeFx == null)
        {
            return;
        }

        foreach (ParticleSystem fx in _burnoutSmokeFx)
        {
            PlayFx(fx);
        }
    }

    private void StopBurnoutSmoke()
    {
        if (_burnoutSmokeFx == null)
        {
            return;
        }

        foreach (ParticleSystem fx in _burnoutSmokeFx)
        {
            StopFx(fx);
        }
    }

    private void ClearBurnoutSmoke()
    {
        if (_burnoutSmokeFx == null)
        {
            return;
        }

        foreach (ParticleSystem fx in _burnoutSmokeFx)
        {
            ClearFx(fx);
        }
    }
}
