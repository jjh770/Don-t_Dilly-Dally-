using DG.Tweening;
using UnityEngine;

// 수술 성공 클리어 연출 3: 헬기 인양.
// 헬기가 위에서 내려오고, 침대에 밧줄을 연결해서 그대로 인양해간다.
public class HelicopterLiftClear : PatientClearBase
{
    [Header("Helicopter Object")]
    [Tooltip("헬기 오브젝트. 비활성 상태로 씬에 배치.")]
    [SerializeField] private Transform _helicopterTransform;

    [Header("Rope Object")]
    [Tooltip("밧줄 오브젝트 (레거시, 단일). 아래 배열이 비었을 때 사용됨. 스케일 Y로 길이 조절.")]
    [SerializeField] private Transform _ropeTransform;

    [Tooltip("밧줄 오브젝트 배열. 2개 이상일 때 여기 할당 (예: 헬기 훅↔침대 손잡이 좌/우 2줄). 비어있으면 위 단일 필드 사용.")]
    [SerializeField] private Transform[] _ropeTransforms;

    [Tooltip("밧줄 연결 시 최종 로컬 스케일 Y. 헬기에서 침대까지 닿는 길이.")]
    [SerializeField] private float _ropeFullLengthScaleY = 1f;

    [Header("Phase 0: Helicopter Descent (헬기 하강)")]
    [Tooltip("헬기가 등장하는 시작 위치 오프셋 (침대 월드 위치 기준). Y를 높게 잡으면 위에서 내려옴.")]
    [SerializeField] private Vector3 _heliStartOffset = new(0f, 15f, 0f);

    [Tooltip("헬기가 호버링하는 위치 오프셋 (침대 월드 위치 기준). 밧줄을 내리기 전 대기 위치.")]
    [SerializeField] private Vector3 _heliHoverOffset = new(0f, 7f, 0f);

    [Tooltip("헬기 하강 시간 (초). 길수록 천천히 내려옴.")]
    [SerializeField] private float _heliDescentDuration = 1.5f;

    [Tooltip("헬기 하강 감속 커브.")]
    [SerializeField] private Ease _heliDescentEase = Ease.OutQuad;

    [Header("Phase 1: Rope Deploy (밧줄 내리기)")]
    [Tooltip("밧줄 내리기 시작 시점 (초). 헬기 하강 완료 타이밍에 맞추세요.")]
    [SerializeField] private float _ropeDeployStartTime = 1.5f;

    [Tooltip("밧줄 내리기 시간 (초). 스케일 Y가 0 → 풀 길이로 변함.")]
    [SerializeField] private float _ropeDeployDuration = 0.8f;

    [Tooltip("밧줄 내리기 감속 커브.")]
    [SerializeField] private Ease _ropeDeployEase = Ease.OutQuad;

    [Header("Phase 2: Hook (연결 흔들림)")]
    [Tooltip("밧줄 연결 시점 (초). 밧줄 내리기 완료 타이밍에 맞추세요.")]
    [SerializeField] private float _hookTime = 2.3f;

    [Tooltip("연결 시 침대 흔들림 시간 (초).")]
    [SerializeField] private float _hookShakeDuration = 0.4f;

    [Tooltip("연결 시 침대 흔들림 강도.")]
    [SerializeField] private float _hookShakeStrength = 0.1f;

    [Tooltip("연결 시 침대 흔들림 진동수.")]
    [SerializeField] private int _hookShakeVibrato = 15;

    [Header("Phase 3: Lift Off (인양)")]
    [Tooltip("인양 시작 시점 (초). 연결 흔들림 후 타이밍에 맞추세요.")]
    [SerializeField] private float _liftStartTime = 2.7f;

    [Tooltip("인양 후 최종 위치 오프셋 (침대 시작 위치 기준). 화면 밖으로 사라질 방향과 거리를 지정하세요.")]
    [SerializeField] private Vector3 _liftExitOffset = new(0f, 15f, 0f);

    [Tooltip("인양 후 헬기 최종 위치 오프셋 (헬기 호버 위치 기준). 침대와 같은 방향으로 설정하세요.")]
    [SerializeField] private Vector3 _heliExitOffset = new(0f, 15f, 0f);

    [Tooltip("인양 시간 (초). 길수록 천천히 올라감.")]
    [SerializeField] private float _liftDuration = 2.0f;

    [Tooltip("인양 가속 커브. InQuad = 점점 빨라짐, Linear = 일정 속도.")]
    [SerializeField] private Ease _liftEase = Ease.InQuad;

    [Tooltip("인양 중 침대 좌우 흔들림 각도 (도).")]
    [SerializeField] private float _liftSwayAngle = 8f;

    [Tooltip("인양 중 침대 한 번 흔들리는 시간 (초). 짧을수록 빠르게 흔들림.")]
    [SerializeField] private float _liftSwayDuration = 0.5f;

    [Header("VFX: Helicopter Rotor Wind (로터 바람)")]
    [Tooltip("헬기 로터 바람 파티클. 바닥에서 퍼지는 바람 효과. null이면 생략.")]
    [SerializeField] private ParticleSystem _rotorWindFx;

    [Tooltip("로터 바람 시작 시점 (초).")]
    [SerializeField] private float _rotorWindStartTime = 0.5f;

    [Tooltip("로터 바람 정지 시점 (초).")]
    [SerializeField] private float _rotorWindStopTime = 3.5f;

    [Header("VFX: Hook Spark (연결 불꽃)")]
    [Tooltip("밧줄 연결 시 불꽃 파티클 (레거시, 단일). 아래 배열이 비었을 때 사용됨.")]
    [SerializeField] private ParticleSystem _hookSparkFx;

    [Tooltip("밧줄 연결 시 불꽃 파티클 배열. 2개 이상일 때 여기 할당 (예: 좌/우 2개). 비어있으면 위 단일 필드 사용.")]
    [SerializeField] private ParticleSystem[] _hookSparkFxs;

    [Header("VFX: Lift Dust (인양 먼지)")]
    [Tooltip("인양 시 바닥에서 일어나는 먼지. null이면 생략.")]
    [SerializeField] private ParticleSystem _liftDustFx;

    private Sequence _sequence;
    private Tween _swayTween;
    private Transform[] _activeRopes;
    private Vector3[] _originalRopeScales;

    // ForceComplete용 초기 상태 캐싱.
    private bool _hasCachedState;
    private Vector3 _cachedRootPosition;
    private Vector3 _cachedBedPosition;
    private Quaternion _cachedBedRotation;

    // 배열이 비었으면 단일 필드를 배열화해서 반환.
    private Transform[] ResolveRopes()
    {
        if (_ropeTransforms != null && _ropeTransforms.Length > 0)
        {
            return _ropeTransforms;
        }

        if (_ropeTransform != null)
        {
            return new[] { _ropeTransform };
        }

        return System.Array.Empty<Transform>();
    }

    // 배열이 비었으면 단일 필드를 배열화해서 반환.
    private ParticleSystem[] ResolveHookSparks()
    {
        if (_hookSparkFxs != null && _hookSparkFxs.Length > 0)
        {
            return _hookSparkFxs;
        }

        if (_hookSparkFx != null)
        {
            return new[] { _hookSparkFx };
        }

        return System.Array.Empty<ParticleSystem>();
    }

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
        _hasCachedState = true;

        Vector3 bedPosition = _cachedBedPosition;

        // 헬기 위치 계산: 침대 위치 + 인스펙터 오프셋.
        Vector3 heliStartPos = bedPosition + _heliStartOffset;
        Vector3 heliHoverPos = bedPosition + _heliHoverOffset;
        Vector3 heliExitPos = heliHoverPos + _heliExitOffset;

        // 침대 인양 목표.
        Vector3 liftExitPos = bedPosition + _liftExitOffset;

        if (_helicopterTransform != null)
        {
            _helicopterTransform.position = heliStartPos;
            _helicopterTransform.gameObject.SetActive(true);
        }

        // 밧줄 초기 상태: 스케일 Y = 0 (접힌 상태).
        _activeRopes = ResolveRopes();
        _originalRopeScales = new Vector3[_activeRopes.Length];

        for (int i = 0; i < _activeRopes.Length; i++)
        {
            Transform rope = _activeRopes[i];
            if (rope == null)
            {
                continue;
            }

            _originalRopeScales[i] = rope.localScale;
            Vector3 startScale = rope.localScale;
            startScale.y = 0f;
            rope.localScale = startScale;
            rope.gameObject.SetActive(false);
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

        // Phase 1: 밧줄 내리기 (모든 밧줄 동시).
        _sequence.InsertCallback(_ropeDeployStartTime, () =>
        {
            foreach (Transform rope in _activeRopes)
            {
                if (rope == null)
                {
                    continue;
                }

                rope.gameObject.SetActive(true);
                rope.DOScaleY(_ropeFullLengthScaleY, _ropeDeployDuration)
                    .SetEase(_ropeDeployEase);
            }
        });

        // Phase 2: 밧줄 연결 — 침대 흔들림 + 불꽃.
        // 주의: patientRoot가 아니라 bedTransform을 흔든다.
        // patientRoot에는 RoomEffect 같은 환경 장식이 같이 있어 함께 흔들리면 안 됨.
        _sequence.InsertCallback(_hookTime, () =>
        {
            foreach (ParticleSystem spark in ResolveHookSparks())
            {
                if (spark != null) PlayFx(spark);
            }
        });

        _sequence.Insert(_hookTime,
            bedTransform.DOShakePosition(_hookShakeDuration, _hookShakeStrength,
                vibrato: _hookShakeVibrato, fadeOut: true));

        // Phase 3: 인양 — 침대(+자식인 환자) + 헬기 동시 상승.
        // 주의: patientRoot를 올리면 형제인 RoomEffect도 같이 올라가므로 bedTransform만 이동.
        _sequence.InsertCallback(_liftStartTime, () => PlayFx(_liftDustFx));

        _sequence.Insert(_liftStartTime,
            bedTransform.DOMove(
                bedTransform.position + _liftExitOffset, _liftDuration)
                .SetEase(_liftEase));

        if (_helicopterTransform != null)
        {
            _sequence.Insert(_liftStartTime,
                _helicopterTransform.DOMove(heliExitPos, _liftDuration)
                    .SetEase(_liftEase));
        }

        // 인양 중 침대 좌우 흔들림.
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

        // 인양 완료 후 정리.
        float liftEndTime = _liftStartTime + _liftDuration;
        _sequence.InsertCallback(liftEndTime, () =>
        {
            _swayTween?.Kill();
            _swayTween = null;
            StopFx(_liftDustFx);
        });

        // 시퀀스 완료 시 파티클 정리만. 위치 원복은 EntranceDirector가 담당.
        _sequence.OnComplete(() =>
        {
            ClearFx(_rotorWindFx);
            foreach (ParticleSystem spark in ResolveHookSparks())
            {
                if (spark != null) ClearFx(spark);
            }
            // 먼지는 StopFx로 방출만 멈춤 — 이미 생성된 입자는 수명대로 페이드아웃.
            // (ForceComplete에서는 ClearFx로 즉시 정리)
            StopFx(_liftDustFx);
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

        // 위치·회전 원복.
        if (_hasCachedState)
        {
            patientRoot.position = _cachedRootPosition;
            bedTransform.position = _cachedBedPosition;
            bedTransform.rotation = _cachedBedRotation;
        }

        if (_helicopterTransform != null)
        {
            _helicopterTransform.DOKill();
            _helicopterTransform.gameObject.SetActive(false);
        }

        if (_activeRopes != null && _originalRopeScales != null)
        {
            for (int i = 0; i < _activeRopes.Length; i++)
            {
                Transform rope = _activeRopes[i];
                if (rope == null)
                {
                    continue;
                }

                rope.DOKill();
                if (i < _originalRopeScales.Length)
                {
                    rope.localScale = _originalRopeScales[i];
                }

                rope.gameObject.SetActive(false);
            }
        }

        ClearFx(_rotorWindFx);
        foreach (ParticleSystem spark in ResolveHookSparks())
        {
            if (spark != null) ClearFx(spark);
        }
        ClearFx(_liftDustFx);
    }
}
