using DG.Tweening;
using UnityEngine;

// 수술 성공 클리어 연출 3: 헬기 인양.
// 헬기가 위에서 내려오고, 침대에 밧줄을 연결해서 그대로 인양해간다.
public class HelicopterLiftClear : PatientClearBase
{
    [Header("Helicopter Object")]
    [Tooltip("헬기 오브젝트. 비활성 상태로 위에 배치.")]
    [SerializeField] private Transform _helicopterTransform;

    [Tooltip("헬기 시작 높이 오프셋 (침대 위치 기준).")]
    [SerializeField] private float _heliStartHeight = 15f;

    [Tooltip("헬기 호버링 높이 (침대 위치 기준).")]
    [SerializeField] private float _heliHoverHeight = 7f;

    [Header("Rope Object")]
    [Tooltip("밧줄 오브젝트. 헬기 자식으로 배치, 비활성. 스케일 Y로 길이 조절.")]
    [SerializeField] private Transform _ropeTransform;

    [Tooltip("밧줄 연결 시 최종 로컬 스케일 Y (헬기에서 침대까지 길이).")]
    [SerializeField] private float _ropeFullLengthScaleY = 1f;

    [Header("Phase 0: Helicopter Descent (헬기 하강)")]
    [Tooltip("헬기 하강 시간 (초).")]
    [SerializeField] private float _heliDescentDuration = 1.5f;

    [Tooltip("헬기 하강 감속 커브.")]
    [SerializeField] private Ease _heliDescentEase = Ease.OutQuad;

    [Header("Phase 1: Rope Deploy (밧줄 내리기)")]
    [Tooltip("밧줄 내리기 시작 시점 (초).")]
    [SerializeField] private float _ropeDeployStartTime = 1.5f;

    [Tooltip("밧줄 내리기 시간 (초).")]
    [SerializeField] private float _ropeDeployDuration = 0.8f;

    [Header("Phase 2: Hook (연결 흔들림)")]
    [Tooltip("밧줄 연결 시점 (초).")]
    [SerializeField] private float _hookTime = 2.3f;

    [Tooltip("연결 시 침대 흔들림 시간 (초).")]
    [SerializeField] private float _hookShakeDuration = 0.4f;

    [Tooltip("연결 시 침대 흔들림 강도.")]
    [SerializeField] private float _hookShakeStrength = 0.1f;

    [Header("Phase 3: Lift Off (인양)")]
    [Tooltip("인양 시작 시점 (초).")]
    [SerializeField] private float _liftStartTime = 2.7f;

    [Tooltip("인양 최종 높이 (미터). 화면 밖으로 올라감.")]
    [SerializeField] private float _liftHeight = 15f;

    [Tooltip("인양 시간 (초).")]
    [SerializeField] private float _liftDuration = 2.0f;

    [Tooltip("인양 중 침대 좌우 흔들림 각도.")]
    [SerializeField] private float _liftSwayAngle = 8f;

    [Tooltip("인양 중 침대 한 번 흔들리는 시간 (초).")]
    [SerializeField] private float _liftSwayDuration = 0.5f;

    [Header("VFX: Helicopter Rotor Wind (로터 바람)")]
    [Tooltip("헬기 로터 바람 파티클. 바닥에서 퍼지는 바람 효과.")]
    [SerializeField] private ParticleSystem _rotorWindFx;

    [Tooltip("로터 바람 시작 시점 (초).")]
    [SerializeField] private float _rotorWindStartTime = 0.5f;

    [Tooltip("로터 바람 정지 시점 (초). 인양 시작 후 잠시 뒤.")]
    [SerializeField] private float _rotorWindStopTime = 3.5f;

    [Header("VFX: Hook Spark (연결 불꽃)")]
    [Tooltip("밧줄 연결 시 불꽃 파티클.")]
    [SerializeField] private ParticleSystem _hookSparkFx;

    [Header("VFX: Lift Dust (인양 먼지)")]
    [Tooltip("인양 시 바닥에서 일어나는 먼지.")]
    [SerializeField] private ParticleSystem _liftDustFx;

    private Sequence _sequence;
    private Tween _swayTween;
    private Vector3 _originalRopeScale;

    public override Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        ForceComplete(patientRoot, bedTransform, patientTransform);

        Vector3 bedPosition = bedTransform.position;

        // 헬기 초기 위치: 침대 바로 위 높은 곳.
        Vector3 heliStartPos = new(bedPosition.x, bedPosition.y + _heliStartHeight, bedPosition.z);
        Vector3 heliHoverPos = new(bedPosition.x, bedPosition.y + _heliHoverHeight, bedPosition.z);

        if (_helicopterTransform != null)
        {
            _helicopterTransform.position = heliStartPos;
            _helicopterTransform.gameObject.SetActive(true);
        }

        // 밧줄 초기 상태: 스케일 Y = 0 (접힌 상태).
        if (_ropeTransform != null)
        {
            _originalRopeScale = _ropeTransform.localScale;
            Vector3 ropeStartScale = _ropeTransform.localScale;
            ropeStartScale.y = 0f;
            _ropeTransform.localScale = ropeStartScale;
            _ropeTransform.gameObject.SetActive(false);
        }

        _sequence = DOTween.Sequence();

        // Phase 0: 헬기 하강.
        if (_helicopterTransform != null)
        {
            _sequence.Append(
                _helicopterTransform.DOMove(heliHoverPos, _heliDescentDuration)
                    .SetEase(_heliDescentEase));
        }

        // 로터 바람 VFX.
        _sequence.InsertCallback(_rotorWindStartTime, () => PlayFx(_rotorWindFx));
        _sequence.InsertCallback(_rotorWindStopTime, () => StopFx(_rotorWindFx));

        // Phase 1: 밧줄 내리기 (스케일 Y 0 → 풀 길이).
        _sequence.InsertCallback(_ropeDeployStartTime, () =>
        {
            if (_ropeTransform != null)
            {
                _ropeTransform.gameObject.SetActive(true);

                Vector3 targetScale = _ropeTransform.localScale;
                targetScale.y = _ropeFullLengthScaleY;
                _ropeTransform.DOScaleY(targetScale.y, _ropeDeployDuration)
                    .SetEase(Ease.OutQuad);
            }
        });

        // Phase 2: 밧줄 연결 — 침대 쿵 흔들림 + 불꽃.
        _sequence.InsertCallback(_hookTime, () =>
        {
            PlayFx(_hookSparkFx);
        });

        _sequence.Insert(_hookTime,
            patientRoot.DOShakePosition(_hookShakeDuration, _hookShakeStrength, vibrato: 15));

        // Phase 3: 인양 — 헬기 + 침대 동시 상승.
        Vector3 liftTarget = bedPosition + Vector3.up * _liftHeight;
        Vector3 heliLiftTarget = heliHoverPos + Vector3.up * _liftHeight;

        _sequence.InsertCallback(_liftStartTime, () =>
        {
            PlayFx(_liftDustFx);
        });

        _sequence.Insert(_liftStartTime,
            patientRoot.DOMove(
                patientRoot.position + Vector3.up * _liftHeight, _liftDuration)
                .SetEase(Ease.InQuad));

        if (_helicopterTransform != null)
        {
            _sequence.Insert(_liftStartTime,
                _helicopterTransform.DOMove(heliLiftTarget, _liftDuration)
                    .SetEase(Ease.InQuad));
        }

        // 인양 중 침대 좌우 흔들림 (Yoyo 루프로 자연스럽게).
        _sequence.InsertCallback(_liftStartTime, () =>
        {
            Quaternion baseRotation = bedTransform.rotation;
            Quaternion swayLeft = baseRotation * Quaternion.Euler(-_liftSwayAngle, 0f, 0f);
            Quaternion swayRight = baseRotation * Quaternion.Euler(_liftSwayAngle, 0f, 0f);

            _swayTween = DOTween.Sequence()
                .Append(bedTransform.DORotateQuaternion(swayLeft, _liftSwayDuration)
                    .SetEase(Ease.InOutSine))
                .Append(bedTransform.DORotateQuaternion(swayRight, _liftSwayDuration)
                    .SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo);
        });

        // 인양 완료 후 먼지 정지.
        float liftEndTime = _liftStartTime + _liftDuration;
        _sequence.InsertCallback(liftEndTime, () =>
        {
            StopFx(_liftDustFx);
            _swayTween?.Kill();
            _swayTween = null;
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

        _swayTween?.Kill();
        _swayTween = null;

        patientRoot.DOKill();
        bedTransform.DOKill();

        if (_helicopterTransform != null)
        {
            _helicopterTransform.DOKill();
            _helicopterTransform.gameObject.SetActive(false);
        }

        if (_ropeTransform != null)
        {
            _ropeTransform.DOKill();
            _ropeTransform.localScale = _originalRopeScale;
            _ropeTransform.gameObject.SetActive(false);
        }

        ClearFx(_rotorWindFx);
        ClearFx(_hookSparkFx);
        ClearFx(_liftDustFx);
    }
}
