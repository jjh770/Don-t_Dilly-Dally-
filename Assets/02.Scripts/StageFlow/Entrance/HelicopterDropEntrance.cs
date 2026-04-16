using DG.Tweening;
using UnityEngine;

// 환자 입장 연출: 헬기 드롭.
// 헬기가 환자(침대)를 매달고 내려와서 자리에 내려놓고, 헬기는 다시 올라가서 사라진다.
public class HelicopterDropEntrance : PatientEntranceBase
{
    [Header("Helicopter Object")]
    [Tooltip("헬기 오브젝트. 비활성 상태로 씬에 배치.")]
    [SerializeField] private Transform _helicopterTransform;

    [Header("Rope Object")]
    [Tooltip("밧줄 오브젝트 (레거시, 단일). 아래 배열이 비었을 때 사용됨.")]
    [SerializeField] private Transform _ropeTransform;

    [Tooltip("밧줄 오브젝트 배열. 2개 이상일 때 여기 할당 (예: 헬기 훅↔침대 손잡이 좌/우 2줄). 비어있으면 위 단일 필드 사용.")]
    [SerializeField] private Transform[] _ropeTransforms;

    [Tooltip("밧줄 최대 로컬 스케일 Y. 헬기에서 침대까지 닿는 길이.")]
    [SerializeField] private float _ropeFullLengthScaleY = 1f;

    [Header("Phase 0: Helicopter Approach (헬기 등장)")]
    [Tooltip("헬기+침대 시작 위치 오프셋 (최종 위치 기준). Y를 높게 잡으면 위에서 내려옴.")]
    [SerializeField] private Vector3 _startOffset = new(0f, 12f, 0f);

    [Tooltip("헬기가 침대 위에서 호버링하는 위치 오프셋 (최종 위치 기준).")]
    [SerializeField] private Vector3 _heliHoverOffset = new(0f, 7f, 0f);

    [Tooltip("헬기+침대 하강 시간 (초).")]
    [SerializeField] private float _descentDuration = 2.0f;

    [Tooltip("헬기 하강 감속 커브.")]
    [SerializeField] private Ease _descentEase = Ease.OutQuad;

    [Header("Phase 1: Bed Touchdown (침대 착지)")]
    [Tooltip("침대 착지 시작 시점 (초). 헬기 하강 완료 타이밍에 맞추세요.")]
    [SerializeField] private float _touchdownStartTime = 2.0f;

    [Tooltip("침대 착지 시간 (초). 밧줄이 늘어나며 침대가 천천히 내려옴.")]
    [SerializeField] private float _touchdownDuration = 1.0f;

    [Tooltip("침대 착지 감속 커브.")]
    [SerializeField] private Ease _touchdownEase = Ease.OutBounce;

    [Header("Phase 2: Unhook (연결 해제)")]
    [Tooltip("연결 해제 시점 (초). 침대 착지 완료 후.")]
    [SerializeField] private float _unhookTime = 3.0f;

    [Tooltip("연결 해제 시 침대 흔들림 시간 (초).")]
    [SerializeField] private float _unhookShakeDuration = 0.3f;

    [Tooltip("연결 해제 시 침대 흔들림 강도.")]
    [SerializeField] private float _unhookShakeStrength = 0.05f;

    [Tooltip("연결 해제 시 침대 흔들림 진동수.")]
    [SerializeField] private int _unhookShakeVibrato = 10;

    [Header("Phase 3: Rope Retract (밧줄 회수)")]
    [Tooltip("밧줄 회수 시작 시점 (초).")]
    [SerializeField] private float _ropeRetractStartTime = 3.3f;

    [Tooltip("밧줄 회수 시간 (초).")]
    [SerializeField] private float _ropeRetractDuration = 0.5f;

    [Tooltip("밧줄 회수 커브.")]
    [SerializeField] private Ease _ropeRetractEase = Ease.InQuad;

    [Header("Phase 4: Helicopter Exit (헬기 퇴장)")]
    [Tooltip("헬기 퇴장 시작 시점 (초).")]
    [SerializeField] private float _heliExitStartTime = 3.8f;

    [Tooltip("헬기 퇴장 위치 오프셋 (호버 위치 기준). 위/옆 어디로든 사라질 방향.")]
    [SerializeField] private Vector3 _heliExitOffset = new(0f, 15f, 0f);

    [Tooltip("헬기 퇴장 시간 (초).")]
    [SerializeField] private float _heliExitDuration = 1.5f;

    [Tooltip("헬기 퇴장 가속 커브.")]
    [SerializeField] private Ease _heliExitEase = Ease.InQuad;

    [Header("Sway (하강 중 흔들림)")]
    [Tooltip("하강 중 침대 좌우 흔들림 각도 (도).")]
    [SerializeField] private float _descentSwayAngle = 6f;

    [Tooltip("침대 한 번 흔들리는 시간 (초).")]
    [SerializeField] private float _descentSwayDuration = 0.4f;

    [Header("VFX: Rotor Wind (로터 바람)")]
    [Tooltip("헬기 로터 바람 파티클. null이면 생략.")]
    [SerializeField] private ParticleSystem _rotorWindFx;

    [Tooltip("로터 바람 시작 시점 (초).")]
    [SerializeField] private float _rotorWindStartTime = 0.5f;

    [Tooltip("로터 바람 정지 시점 (초).")]
    [SerializeField] private float _rotorWindStopTime = 4.0f;

    [Header("VFX: Unhook Spark (연결 해제 불꽃)")]
    [Tooltip("연결 해제 시 불꽃 파티클. null이면 생략.")]
    [SerializeField] private ParticleSystem _unhookSparkFx;

    [Header("VFX: Landing Dust (착지 먼지)")]
    [Tooltip("침대 착지 시 바닥 먼지 파티클. null이면 생략.")]
    [SerializeField] private ParticleSystem _landingDustFx;

    [Tooltip("착지 먼지 시작 시점 (초).")]
    [SerializeField] private float _landingDustStartTime = 2.8f;

    private Sequence _sequence;
    private Tween _swayTween;
    private Transform[] _activeRopes;
    private Vector3[] _originalRopeScales;

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

    public override Sequence Play(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation)
    {
        ForceComplete(bedTransform, finalPosition, finalRotation);

        // 침대 시작 위치: 최종 위치 + 오프셋 (헬기와 같이 높은 곳에서 시작).
        Vector3 startPosition = finalPosition + _startOffset;
        bedTransform.position = startPosition;
        bedTransform.rotation = finalRotation;

        // 헬기 위치 계산.
        Vector3 heliHoverPos = finalPosition + _heliHoverOffset;
        Vector3 heliStartPos = heliHoverPos + _startOffset;
        Vector3 heliExitPos = heliHoverPos + _heliExitOffset;

        if (_helicopterTransform != null)
        {
            _helicopterTransform.position = heliStartPos;
            _helicopterTransform.gameObject.SetActive(true);
        }

        // 밧줄 초기 상태: 풀 길이 (처음부터 연결된 상태).
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
            Vector3 scale = rope.localScale;
            scale.y = _ropeFullLengthScaleY;
            rope.localScale = scale;
            rope.gameObject.SetActive(true);
        }

        _sequence = DOTween.Sequence();

        // Phase 0: 헬기+침대 동시 하강.
        _sequence.Append(
            bedTransform.DOMove(finalPosition, _descentDuration)
                .SetEase(_descentEase));

        if (_helicopterTransform != null)
        {
            _sequence.Insert(0f,
                _helicopterTransform.DOMove(heliHoverPos, _descentDuration)
                    .SetEase(_descentEase));
        }

        // 하강 중 침대 흔들림.
        _sequence.InsertCallback(0f, () =>
        {
            Quaternion baseRotation = finalRotation;
            Quaternion swayLeft = baseRotation * Quaternion.Euler(-_descentSwayAngle, 0f, 0f);
            Quaternion swayRight = baseRotation * Quaternion.Euler(_descentSwayAngle, 0f, 0f);

            _swayTween = DOTween.Sequence()
                .Append(bedTransform.DORotateQuaternion(swayLeft, _descentSwayDuration)
                    .SetEase(Ease.InOutSine))
                .Append(bedTransform.DORotateQuaternion(swayRight, _descentSwayDuration)
                    .SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo);
        });

        // 로터 바람 VFX.
        _sequence.InsertCallback(_rotorWindStartTime, () => PlayFx(_rotorWindFx));
        _sequence.InsertCallback(_rotorWindStopTime, () => StopFx(_rotorWindFx));

        // Phase 1: 침대 착지 (이미 DOMove로 최종 위치까지 갔지만, 흔들림 멈추기).
        _sequence.InsertCallback(_touchdownStartTime, () =>
        {
            _swayTween?.Kill();
            _swayTween = null;

            // 착지 시 회전을 최종 회전으로 정렬.
            bedTransform.DORotateQuaternion(finalRotation, _touchdownDuration * 0.5f)
                .SetEase(Ease.OutQuad);
        });

        // 착지 먼지.
        _sequence.InsertCallback(_landingDustStartTime, () => PlayFx(_landingDustFx));

        // Phase 2: 연결 해제 — 살짝 흔들림 + 불꽃.
        _sequence.InsertCallback(_unhookTime, () => PlayFx(_unhookSparkFx));

        _sequence.Insert(_unhookTime,
            bedTransform.DOShakePosition(_unhookShakeDuration, _unhookShakeStrength,
                vibrato: _unhookShakeVibrato, fadeOut: true));

        // Phase 3: 밧줄 회수 (스케일 Y → 0, 모든 밧줄 동시).
        _sequence.InsertCallback(_ropeRetractStartTime, () =>
        {
            foreach (Transform rope in _activeRopes)
            {
                if (rope == null)
                {
                    continue;
                }

                Transform capturedRope = rope;
                capturedRope.DOScaleY(0f, _ropeRetractDuration)
                    .SetEase(_ropeRetractEase)
                    .OnComplete(() => capturedRope.gameObject.SetActive(false));
            }
        });

        // Phase 4: 헬기 퇴장.
        if (_helicopterTransform != null)
        {
            _sequence.Insert(_heliExitStartTime,
                _helicopterTransform.DOMove(heliExitPos, _heliExitDuration)
                    .SetEase(_heliExitEase));
        }

        // 시퀀스 완료 시 정리.
        _sequence.OnComplete(() =>
        {
            if (_helicopterTransform != null)
            {
                _helicopterTransform.gameObject.SetActive(false);
            }

            ClearFx(_rotorWindFx);
            ClearFx(_unhookSparkFx);
            ClearFx(_landingDustFx);
        });

        return _sequence;
    }

    public override void ForceComplete(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation)
    {
        _sequence?.Kill();
        _sequence = null;

        _swayTween?.Kill();
        _swayTween = null;

        bedTransform.DOKill();
        bedTransform.position = finalPosition;
        bedTransform.rotation = finalRotation;

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
        ClearFx(_unhookSparkFx);
        ClearFx(_landingDustFx);
    }
}
