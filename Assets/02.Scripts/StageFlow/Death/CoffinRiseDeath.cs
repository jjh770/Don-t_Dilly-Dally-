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

    [Tooltip("침대→관 교체 시점 (초). 연기가 짙을 때.")]
    [SerializeField] private float _swapTime = 0.8f;

    [Header("Phase 3: Coffin Rise")]
    [Tooltip("관이 바닥 아래에서 시작하는 깊이 (미터).")]
    [SerializeField] private float _coffinStartDepth = 3f;

    [Tooltip("관 상승 시간 (초). 천천히 떨리면서 올라옴.")]
    [SerializeField] private float _riseDuration = 1.7f;

    [Tooltip("관 상승 중 떨림 강도.")]
    [SerializeField] private float _riseShakeStrength = 0.03f;

    [Header("Phase 4: Patient Merge")]
    [Tooltip("환자가 관에 합류하는 시점 (초). 관 바닥이 환자 높이에 닿을 때.")]
    [SerializeField] private float _patientMergeTime = 1.8f;

    [Header("Phase 5: Lid Close")]
    [Tooltip("뚜껑 닫힘 시작 시점 (초).")]
    [SerializeField] private float _lidCloseStartTime = 2.8f;

    [Tooltip("뚜껑 닫힘 시간 (초).")]
    [SerializeField] private float _lidCloseDuration = 0.7f;

    [Tooltip("뚜껑 열린 상태의 회전 각도 (도).")]
    [SerializeField] private float _lidOpenAngle = -90f;

    [Header("VFX: Dirt")]
    [Tooltip("관 상승 중 흙먼지 파티클. Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _dirtFx;

    [Tooltip("뚜껑 닫힐 때 쿵 먼지 파티클. Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _lidDustFx;

    private Sequence _sequence;
    private Vector3 _coffinFinalPosition;
    private Transform _originalPatientParent;

    public override Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        ForceComplete(patientRoot, bedTransform, patientTransform);

        _originalPatientParent = patientTransform.parent;

        // 관의 최종 위치를 침대 위치 기준으로 설정.
        _coffinFinalPosition = bedTransform.position;
        Vector3 coffinStartPosition = _coffinFinalPosition + Vector3.down * _coffinStartDepth;

        // 관 초기 상태: 바닥 아래, 뚜껑 열린 상태.
        if (_coffinTransform != null)
        {
            _coffinTransform.position = coffinStartPosition;
        }

        if (_coffinLid != null)
        {
            _coffinLid.localRotation = Quaternion.Euler(_lidOpenAngle, 0f, 0f);
        }

        _sequence = DOTween.Sequence();

        // Phase 0: 전체 떨림.
        _sequence.Append(
            patientRoot.DOShakePosition(_trembleDuration, _trembleStrength, vibrato: 20));

        // Phase 1: 연기 폭발.
        _sequence.InsertCallback(_smokeStartTime, () => PlayFx(_smokeFx));

        // Phase 2: 연기 속에서 침대→관 교체.
        _sequence.InsertCallback(_swapTime, () =>
        {
            bedTransform.gameObject.SetActive(false);

            if (_coffinTransform != null)
            {
                _coffinTransform.gameObject.SetActive(true);
            }

            // 흙먼지 시작.
            PlayFx(_dirtFx);
        });

        // Phase 3: 관 상승 (떨리면서 천천히).
        if (_coffinTransform != null)
        {
            _sequence.Insert(_swapTime,
                _coffinTransform.DOMove(_coffinFinalPosition, _riseDuration)
                    .SetEase(Ease.Linear));

            _sequence.Insert(_swapTime,
                _coffinTransform.DOShakePosition(_riseDuration, _riseShakeStrength, vibrato: 15)
                    .SetRelative(true));
        }

        // Phase 4: 환자가 관에 합류 (관 바닥이 환자에 닿을 때).
        _sequence.InsertCallback(_patientMergeTime, () =>
        {
            if (_coffinTransform != null)
            {
                patientTransform.SetParent(_coffinTransform);
            }
        });

        // Phase 5: 뚜껑 닫힘.
        if (_coffinLid != null)
        {
            _sequence.Insert(_lidCloseStartTime,
                _coffinLid.DOLocalRotate(Vector3.zero, _lidCloseDuration)
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

        // 환자를 원래 부모로 복원.
        if (_originalPatientParent != null && patientTransform.parent != _originalPatientParent)
        {
            patientTransform.SetParent(_originalPatientParent);
        }

        ClearFx(_smokeFx);
        ClearFx(_dirtFx);
        ClearFx(_lidDustFx);
    }
}
