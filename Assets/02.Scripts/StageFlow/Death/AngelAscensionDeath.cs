using DG.Tweening;
using UnityEngine;

public class AngelAscensionDeath : PatientDeathBase
{
    [Header("Phase 0: Light Beam")]
    [Tooltip("빛 기둥 오브젝트. 환자 위치에 미리 배치, 비활성.")]
    [SerializeField] private GameObject _lightBeam;

    [Header("Phase 1: Halo")]
    [Tooltip("후광 오브젝트. 환자 머리 위에 배치, 비활성.")]
    [SerializeField] private Transform _halo;

    [Tooltip("후광 등장 시점 (초).")]
    [SerializeField] private float _haloAppearTime = 0.3f;

    [Tooltip("후광 스케일 애니메이션 시간 (초).")]
    [SerializeField] private float _haloScaleDuration = 0.3f;

    [Header("Phase 2: Wings")]
    [Tooltip("날개 오브젝트. 환자 등 뒤에 배치, 비활성.")]
    [SerializeField] private Transform _wings;

    [Tooltip("날개 등장 시점 (초).")]
    [SerializeField] private float _wingsAppearTime = 0.5f;

    [Tooltip("날개 스케일 애니메이션 시간 (초).")]
    [SerializeField] private float _wingsScaleDuration = 0.3f;

    [Header("Phase 3: Wing Flap")]
    [Tooltip("날개 펄럭임 시작 시점 (초).")]
    [SerializeField] private float _flapStartTime = 0.7f;

    [Tooltip("날개 펄럭임 각도 (도).")]
    [SerializeField] private float _flapAngle = 15f;

    [Tooltip("한 번 펄럭이는 시간 (초).")]
    [SerializeField] private float _flapDuration = 0.4f;

    [Header("Phase 4: Ascension")]
    [Tooltip("상승 시작 시점 (초).")]
    [SerializeField] private float _ascensionStartTime = 1.0f;

    [Tooltip("상승 높이 (미터).")]
    [SerializeField] private float _ascensionHeight = 8f;

    [Tooltip("상승 시간 (초).")]
    [SerializeField] private float _ascensionDuration = 2.0f;

    [Tooltip("상승 끝 스케일 (작아지면서 사라짐).")]
    [SerializeField] private float _endScale = 0.1f;

    [Header("VFX: Sparkle Trail")]
    [Tooltip("상승 중 반짝이 파티클. 환자 자식으로 배치, Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _sparkleFx;

    [Header("Phase 5: Fade Out")]
    [Tooltip("빛 기둥 소멸 시점 (초).")]
    [SerializeField] private float _beamFadeTime = 3.0f;

    [Tooltip("빛 기둥 페이드 시간 (초).")]
    [SerializeField] private float _beamFadeDuration = 0.5f;

    private Sequence _sequence;
    private Tween _flapTween;
    private Transform _originalPatientParent;
    private Vector3 _originalPatientScale;

    public override Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        ForceComplete(patientRoot, bedTransform, patientTransform);

        _originalPatientParent = patientTransform.parent;
        _originalPatientScale = patientTransform.localScale;

        // 후광, 날개 초기 상태: 스케일 0.
        if (_halo != null)
        {
            _halo.localScale = Vector3.zero;
        }

        if (_wings != null)
        {
            _wings.localScale = Vector3.zero;
        }

        _sequence = DOTween.Sequence();

        // Phase 0: 빛 기둥 활성화.
        _sequence.InsertCallback(0f, () =>
        {
            if (_lightBeam != null)
            {
                _lightBeam.SetActive(true);
            }
        });

        // Phase 1: 후광 팝인.
        _sequence.InsertCallback(_haloAppearTime, () =>
        {
            if (_halo != null)
            {
                _halo.gameObject.SetActive(true);
                _halo.DOScale(Vector3.one, _haloScaleDuration).SetEase(Ease.OutBack);
            }
        });

        // Phase 2: 날개 팝인.
        _sequence.InsertCallback(_wingsAppearTime, () =>
        {
            if (_wings != null)
            {
                _wings.gameObject.SetActive(true);
                _wings.DOScale(Vector3.one, _wingsScaleDuration).SetEase(Ease.OutBack);
            }
        });

        // Phase 3: 날개 펄럭임 (핑퐁 루프).
        _sequence.InsertCallback(_flapStartTime, () =>
        {
            if (_wings != null)
            {
                _flapTween = _wings
                    .DOLocalRotate(new Vector3(_flapAngle, 0f, 0f), _flapDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        });

        // Phase 4: 누운 채로 상승.
        // 환자를 patientRoot에서 분리하여 독립적으로 올림.
        _sequence.InsertCallback(_ascensionStartTime, () =>
        {
            patientTransform.SetParent(null, worldPositionStays: true);
            PlayFx(_sparkleFx);
        });

        Vector3 ascensionTarget = patientTransform.position + Vector3.up * _ascensionHeight;
        _sequence.Insert(_ascensionStartTime,
            patientTransform.DOMove(ascensionTarget, _ascensionDuration)
                .SetEase(Ease.InQuad));

        _sequence.Insert(_ascensionStartTime,
            patientTransform.DOScale(_originalPatientScale * _endScale, _ascensionDuration)
                .SetEase(Ease.InQuad));

        // Phase 5: 빛 기둥 페이드.
        _sequence.InsertCallback(_beamFadeTime, () =>
        {
            if (_lightBeam != null)
            {
                _lightBeam.transform
                    .DOScale(Vector3.zero, _beamFadeDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => _lightBeam.SetActive(false));
            }
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

        _flapTween?.Kill();
        _flapTween = null;

        if (_halo != null)
        {
            _halo.DOKill();
        }

        if (_wings != null)
        {
            _wings.DOKill();
        }

        if (_lightBeam != null)
        {
            _lightBeam.transform.DOKill();
        }

        patientTransform.DOKill();

        // 환자를 원래 부모로 복원.
        if (_originalPatientParent != null && patientTransform.parent != _originalPatientParent)
        {
            patientTransform.SetParent(_originalPatientParent, worldPositionStays: false);
            patientTransform.localScale = _originalPatientScale;
        }

        ClearFx(_sparkleFx);

        if (_lightBeam != null)
        {
            _lightBeam.SetActive(false);
        }

        if (_halo != null)
        {
            _halo.gameObject.SetActive(false);
        }

        if (_wings != null)
        {
            _wings.gameObject.SetActive(false);
        }
    }
}
