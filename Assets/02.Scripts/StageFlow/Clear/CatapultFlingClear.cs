using DG.Tweening;
using UnityEngine;

// 수술 성공 클리어 연출 2: 투석기 사출.
// 침대가 바짝 세워지고, 환자가 공중제비하며 날아간다.
// 침대는 바운스 복귀 후 왼쪽으로 퇴장.
public class CatapultFlingClear : PatientClearBase
{
    [Header("Phase 0: Charge (충전 떨림)")]
    [Tooltip("발사 전 충전 떨림 시간 (초).")]
    [SerializeField] private float _chargeDuration = 0.8f;

    [Tooltip("충전 떨림 강도.")]
    [SerializeField] private float _chargeShakeStrength = 0.06f;

    [Tooltip("충전 떨림 진동수.")]
    [SerializeField] private int _chargeVibrato = 25;

    [Header("Phase 1: Catapult (침대 세우기)")]
    [Tooltip("침대가 세워지기 시작하는 시점 (초).")]
    [SerializeField] private float _catapultStartTime = 0.8f;

    [Tooltip("투석기 피봇 포인트. 빈 오브젝트를 침대 발쪽(회전축)에 배치하세요.")]
    [SerializeField] private Transform _catapultPivot;

    [Tooltip("침대 세우기 각도 (도).")]
    [SerializeField] private float _catapultAngleDeg = 80f;

    [Tooltip("회전 축. 침대 Y90 기준 X축이 기본.")]
    [SerializeField] private Vector3 _catapultAxis = Vector3.right;

    [Tooltip("침대 세우기 시간 (초). 짧을수록 급격하게 세워짐.")]
    [SerializeField] private float _catapultDuration = 0.25f;

    [Header("Phase 2: Patient Fling (환자 사출)")]
    [Tooltip("환자 사출 시점 (초). 침대가 세워진 직후.")]
    [SerializeField] private float _flingStartTime = 1.05f;

    [Tooltip("환자 비행 최고점 높이 (미터).")]
    [SerializeField] private float _flingPeakHeight = 8f;

    [Tooltip("환자 비행 수평 거리 (미터). X축.")]
    [SerializeField] private float _flingHorizontalDistance = 10f;

    [Tooltip("환자 비행 총 시간 (초).")]
    [SerializeField] private float _flingDuration = 1.5f;

    [Tooltip("환자 공중제비 회전 수. 360도 * 횟수.")]
    [SerializeField] private int _flipCount = 3;

    [Header("Phase 3: Bed Bounce & Slide (바운스 복귀 + 퇴장)")]
    [Tooltip("침대가 제자리로 튕기기 시작하는 시점 (초).")]
    [SerializeField] private float _bounceStartTime = 1.3f;

    [Tooltip("바운스 복귀 시간 (초). OutBounce로 2~3번 통통 튕김.")]
    [SerializeField] private float _bounceDuration = 1.0f;

    [Tooltip("바운스 후 왼쪽 슬라이드 시작 시점 (초).")]
    [SerializeField] private float _slideStartTime = 2.5f;

    [Tooltip("왼쪽 슬라이드 오프셋 (위치). 회전 없이 이동만.")]
    [SerializeField] private Vector3 _slideOffset = new(-8f, 0f, 0f);

    [Tooltip("왼쪽 슬라이드 시간 (초).")]
    [SerializeField] private float _slideDuration = 0.6f;

    [Header("VFX: Charge Dust (충전 먼지)")]
    [Tooltip("충전 중 바닥 먼지 파티클.")]
    [SerializeField] private ParticleSystem _chargeDustFx;

    [Header("VFX: Fling Trail (사출 잔상)")]
    [Tooltip("환자 사출 시 반짝이 트레일. 환자 자식으로 배치.")]
    [SerializeField] private ParticleSystem _flingTrailFx;

    [Header("VFX: Catapult Smoke (투석기 연기)")]
    [Tooltip("침대가 세워질 때 바닥 연기.")]
    [SerializeField] private ParticleSystem _catapultSmokeFx;

    private Sequence _sequence;
    private Transform _originalPatientParent;
    private Vector3 _originalPatientScale;

    // ForceComplete용 초기 상태 캐싱.
    private bool _hasCachedState;
    private Vector3 _cachedRootPosition;
    private Vector3 _cachedBedPosition;
    private Quaternion _cachedBedRotation;
    private Vector3 _cachedPatientLocalPosition;
    private Quaternion _cachedPatientLocalRotation;

    public override Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        // ForceComplete 먼저 — 이전 연출 중간 상태를 정리한 뒤 캐싱.
        ForceComplete(patientRoot, bedTransform, patientTransform);

        _cachedRootPosition = patientRoot.position;
        _cachedBedPosition = bedTransform.position;
        _cachedBedRotation = bedTransform.rotation;
        _cachedPatientLocalPosition = patientTransform.localPosition;
        _cachedPatientLocalRotation = patientTransform.localRotation;
        _hasCachedState = true;

        _originalPatientParent = patientTransform.parent;
        _originalPatientScale = patientTransform.localScale;

        Vector3 bedStartPos = _cachedBedPosition;
        Quaternion bedStartRot = _cachedBedRotation;

        _sequence = DOTween.Sequence();

        // Phase 0: 충전 떨림.
        _sequence.Append(
            patientRoot.DOShakePosition(_chargeDuration, _chargeShakeStrength,
                vibrato: _chargeVibrato, fadeOut: true));

        _sequence.InsertCallback(0f, () => PlayFx(_chargeDustFx));
        _sequence.InsertCallback(_chargeDuration, () => StopFx(_chargeDustFx));

        // Phase 1: 침대 바짝 세우기 (투석기).
        Vector3 pivotPoint = _catapultPivot != null ? _catapultPivot.position : bedStartPos;
        Vector3 rotAxis = _catapultAxis.normalized;
        float rotated = 0f;

        _sequence.Insert(_catapultStartTime,
            DOVirtual.Float(0f, _catapultAngleDeg, _catapultDuration, delta =>
            {
                float step = delta - rotated;
                bedTransform.RotateAround(pivotPoint, rotAxis, step);
                rotated = delta;
            }).SetEase(Ease.OutBack));

        _sequence.InsertCallback(_catapultStartTime, () => PlayFx(_catapultSmokeFx));

        // Phase 2: 환자 사출.
        // 분리 전 환자의 회전 축을 캡처해서 그 축으로 공중제비.
        Vector3 spinAxis = Vector3.right;
        Quaternion spinBaseRotation = Quaternion.identity;

        _sequence.InsertCallback(_flingStartTime, () =>
        {
            // 분리 전 환자의 right 축 = 공중제비 회전 축.
            spinAxis = patientTransform.right;
            spinBaseRotation = patientTransform.rotation;

            patientTransform.SetParent(null, worldPositionStays: true);
            PlayFx(_flingTrailFx);
        });

        // 포물선 비행.
        Vector3 flingStart = patientTransform.position;
        Vector3 flingPeak = flingStart + new Vector3(
            _flingHorizontalDistance * 0.5f,
            _flingPeakHeight,
            0f);
        Vector3 flingEnd = flingStart + new Vector3(
            _flingHorizontalDistance,
            _flingPeakHeight * 0.3f,
            0f);

        Vector3[] flingPath = { flingStart, flingPeak, flingEnd };

        _sequence.Insert(_flingStartTime,
            patientTransform.DOPath(flingPath, _flingDuration, PathType.CatmullRom)
                .SetEase(Ease.Linear));

        // 공중제비 — 캡처한 축으로 수동 회전.
        float flipAngle = 360f * _flipCount;
        float flipProgress = 0f;

        _sequence.Insert(_flingStartTime,
            DOVirtual.Float(0f, flipAngle, _flingDuration, angle =>
            {
                float step = angle - flipProgress;
                patientTransform.rotation = Quaternion.AngleAxis(angle, spinAxis) * spinBaseRotation;
                flipProgress = angle;
            }).SetEase(Ease.Linear));

        // Phase 3a: 침대 바운스 복귀.
        float bounceProgress = 0f;

        _sequence.Insert(_bounceStartTime,
            DOVirtual.Float(0f, _catapultAngleDeg, _bounceDuration, delta =>
            {
                float step = delta - bounceProgress;
                bedTransform.RotateAround(pivotPoint, rotAxis, -step);
                bounceProgress = delta;
            }).SetEase(Ease.OutBounce));

        // Phase 3b: 왼쪽 슬라이드.
        Vector3 slideTarget = bedStartPos + _slideOffset;

        _sequence.Insert(_slideStartTime,
            bedTransform.DOMove(slideTarget, _slideDuration)
                .SetEase(Ease.InQuad));

        // 시퀀스 끝나면 파티클 정리만. 위치 원복은 EntranceDirector가 담당.
        _sequence.OnComplete(() =>
        {
            ClearFx(_chargeDustFx);
            ClearFx(_flingTrailFx);
            ClearFx(_catapultSmokeFx);
        });

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
        patientTransform.DOKill();

        // 환자를 원래 부모로 복원.
        if (_originalPatientParent != null && patientTransform.parent != _originalPatientParent)
        {
            patientTransform.SetParent(_originalPatientParent, worldPositionStays: false);
            patientTransform.localScale = _originalPatientScale;
        }

        // RotateAround/DOShake로 변경된 위치·회전 원복.
        if (_hasCachedState)
        {
            patientRoot.position = _cachedRootPosition;
            bedTransform.position = _cachedBedPosition;
            bedTransform.rotation = _cachedBedRotation;
            patientTransform.localPosition = _cachedPatientLocalPosition;
            patientTransform.localRotation = _cachedPatientLocalRotation;
        }

        ClearFx(_chargeDustFx);
        ClearFx(_flingTrailFx);
        ClearFx(_catapultSmokeFx);
    }
}
