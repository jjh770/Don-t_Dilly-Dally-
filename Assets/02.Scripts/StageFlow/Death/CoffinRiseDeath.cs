using DG.Tweening;
using UnityEngine;

public class CoffinRiseDeath : PatientDeathBase
{
    [Header("Phase 0: Trembling")]
    [SerializeField] private float _trembleDuration = 0.3f;
    [SerializeField] private float _trembleStrength = 0.05f;

    [Header("Phase 1: Smoke Burst")]
    [Tooltip("침대+환자를 가리는 큰 연기 파티클. Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _smokeFx;

    [Tooltip("연기 재생 시작 시점 (초).")]
    [SerializeField] private float _smokeStartTime = 0.3f;

    [Header("Phase 2: Coffin Swap")]
    [Tooltip("관 오브젝트. PatientObject에 미리 배치, 비활성. 초기 위치는 바닥 아래.")]
    [SerializeField] private Transform _coffinTransform;

    [Tooltip("관 뚜껑. 관의 자식, 힌지 피봇 기준 회전.")]
    [SerializeField] private Transform _coffinLid;

    [Tooltip("침대 사라지는 시점 (초).")]
    [SerializeField] private float _bedHideTime = 0.8f;

    [Tooltip("관 등장 시점 (초).")]
    [SerializeField] private float _coffinAppearTime = 1.2f;

    [Header("Phase 3: Coffin Rise")]
    [Tooltip("관이 바닥 아래에서 시작하는 깊이 (미터).")]
    [SerializeField] private float _coffinStartDepth = 3f;

    [Tooltip("관의 최종 Y 위치.")]
    [SerializeField] private float _coffinFinalY = 0.1f;

    [Tooltip("관 상승 시간 (초). 천천히 떨리면서 올라옴.")]
    [SerializeField] private float _riseDuration = 1.7f;

    [Tooltip("관 상승 중 떨림 강도.")]
    [SerializeField] private float _riseShakeStrength = 0.03f;

    [Header("Phase 4: Patient Merge")]
    [Tooltip("환자가 관에 합류하는 시점 (초).")]
    [SerializeField] private float _patientMergeTime = 1.8f;

    [Tooltip("침대 사라진 후 환자의 Y 위치 (바닥).")]
    [SerializeField] private float _patientGroundY = 0f;

    [Tooltip("환자가 관에 합류한 후 최종 Y 위치.")]
    [SerializeField] private float _patientFinalY = 0.1f;

    [Header("Phase 5: Lid Close")]
    [Tooltip("뚜껑 닫힘 시작 시점 (초).")]
    [SerializeField] private float _lidCloseStartTime = 2.8f;

    [Tooltip("뚜껑 닫힘 시간 (초).")]
    [SerializeField] private float _lidCloseDuration = 0.7f;

    [Tooltip("뚜껑 열린 상태 로컬 위치. (뚜껑 선택 → Transform 값 복사)")]
    [SerializeField] private Vector3 _lidOpenLocalPosition;

    [Tooltip("뚜껑 열린 상태 로컬 회전. (뚜껑 선택 → Transform Rotation 값 복사)")]
    [SerializeField] private Vector3 _lidOpenLocalEuler;

    [Tooltip("뚜껑 닫힌 상태 로컬 위치.")]
    [SerializeField] private Vector3 _lidClosedLocalPosition;

    [Tooltip("뚜껑 닫힌 상태 로컬 회전.")]
    [SerializeField] private Vector3 _lidClosedLocalEuler;

    [Header("VFX: Dirt")]
    [Tooltip("관 상승 중 흙먼지 파티클. Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _dirtFx;

    [Tooltip("뚜껑 닫힐 때 쿵 먼지 파티클. Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _lidDustFx;

    private Sequence _sequence;
    private Vector3 _coffinFinalPosition;
    private Transform _originalSmokeFxParent;
    private Transform _originalPatientParent;

    public override Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        ForceComplete(patientRoot, bedTransform, patientTransform);

        // 관의 최종 위치: XZ는 침대 기준, Y는 인스펙터 지정값.
        _coffinFinalPosition = bedTransform.position;
        _coffinFinalPosition.y = _coffinFinalY;
        Vector3 coffinStartPosition = _coffinFinalPosition + Vector3.down * _coffinStartDepth;

        // 관 초기 상태: 바닥 아래, 뚜껑 열린 상태.
        if (_coffinTransform != null)
        {
            _coffinTransform.position = coffinStartPosition;
        }

        if (_coffinLid != null)
        {
            _coffinLid.localPosition = _lidOpenLocalPosition;
            _coffinLid.localRotation = Quaternion.Euler(_lidOpenLocalEuler);
        }

        _sequence = DOTween.Sequence();

        // Phase 0: 전체 떨림.
        _sequence.Append(
            patientRoot.DOShakePosition(_trembleDuration, _trembleStrength, vibrato: 20));

        // Phase 1: 연기 폭발.
        // 연기 FX가 침대 자식이면 침대 비활성화 시 같이 사라지므로,
        // 재생 전에 월드 공간으로 분리.
        _sequence.InsertCallback(_smokeStartTime, () =>
        {
            if (_smokeFx != null)
            {
                _originalSmokeFxParent = _smokeFx.transform.parent;
                _smokeFx.transform.SetParent(null, worldPositionStays: true);
            }

            PlayFx(_smokeFx);
        });

        // Phase 2a: 침대 사라짐 + 환자 바닥으로.
        // 환자가 침대의 자식이면 침대 비활성화 시 같이 사라지므로,
        // 먼저 월드 공간으로 분리.
        _sequence.InsertCallback(_bedHideTime, () =>
        {
            _originalPatientParent = patientTransform.parent;
            patientTransform.SetParent(null, worldPositionStays: true);

            bedTransform.gameObject.SetActive(false);

            Vector3 patientPos = patientTransform.position;
            patientPos.y = _patientGroundY;
            patientTransform.position = patientPos;
        });

        // Phase 2b: 관 등장.
        _sequence.InsertCallback(_coffinAppearTime, () =>
        {
            if (_coffinTransform != null)
            {
                _coffinTransform.gameObject.SetActive(true);
            }

            PlayFx(_dirtFx);
        });

        // Phase 3: 관 상승 (떨리면서 천천히).
        // DOMove와 DOShakePosition은 동일 Transform.position을 매 프레임 덮어쓰므로 충돌.
        // DOPath + 수동 흔들림 오프셋으로 해결.
        if (_coffinTransform != null)
        {
            Vector3 shakeOffset = Vector3.zero;

            _sequence.Insert(_coffinAppearTime,
                _coffinTransform.DOMove(_coffinFinalPosition, _riseDuration)
                    .SetEase(Ease.Linear)
                    .OnUpdate(() =>
                    {
                        // 이전 프레임 흔들림 되돌리기.
                        _coffinTransform.position -= shakeOffset;

                        // 새 흔들림 적용.
                        shakeOffset = new Vector3(
                            Random.Range(-_riseShakeStrength, _riseShakeStrength),
                            Random.Range(-_riseShakeStrength, _riseShakeStrength),
                            Random.Range(-_riseShakeStrength, _riseShakeStrength));

                        _coffinTransform.position += shakeOffset;
                    })
                    .OnComplete(() =>
                    {
                        // 흔들림 잔여값 제거.
                        _coffinTransform.position -= shakeOffset;
                    }));
        }

        // Phase 4: 환자가 관 바닥 위치까지 살짝 떠오름 (부모 변경 없이 Y만 이동).
        float mergeMoveDuration = (_lidCloseStartTime - _patientMergeTime) * 0.5f;
        _sequence.Insert(_patientMergeTime,
            patientTransform.DOMoveY(_patientFinalY, mergeMoveDuration)
                .SetEase(Ease.OutQuad));

        // Phase 5: 뚜껑 닫힘 (위치 + 회전 동시 트윈).
        if (_coffinLid != null)
        {
            _sequence.Insert(_lidCloseStartTime,
                _coffinLid.DOLocalMove(_lidClosedLocalPosition, _lidCloseDuration)
                    .SetEase(Ease.InQuart));

            _sequence.Insert(_lidCloseStartTime,
                _coffinLid.DOLocalRotateQuaternion(
                    Quaternion.Euler(_lidClosedLocalEuler), _lidCloseDuration)
                    .SetEase(Ease.InQuart));
        }

        // 뚜껑 닫힐 때 쿵 먼지.
        float lidCloseEndTime = _lidCloseStartTime + _lidCloseDuration;
        _sequence.InsertCallback(lidCloseEndTime, () =>
        {
            PlayFx(_lidDustFx);
            StopFx(_dirtFx);
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

        if (_coffinTransform != null)
        {
            _coffinTransform.DOKill();
        }

        if (_coffinLid != null)
        {
            _coffinLid.DOKill();
        }

        patientTransform.DOKill();

        // 환자를 원래 부모(침대)로 복원.
        if (_originalPatientParent != null && patientTransform.parent != _originalPatientParent)
        {
            bedTransform.gameObject.SetActive(true);
            patientTransform.SetParent(_originalPatientParent, worldPositionStays: false);
        }

        ClearFx(_smokeFx);

        // 연기 FX를 원래 부모(침대)로 복원.
        if (_originalSmokeFxParent != null && _smokeFx != null
            && _smokeFx.transform.parent != _originalSmokeFxParent)
        {
            _smokeFx.transform.SetParent(_originalSmokeFxParent, worldPositionStays: true);
        }
        ClearFx(_dirtFx);
        ClearFx(_lidDustFx);
    }
}
