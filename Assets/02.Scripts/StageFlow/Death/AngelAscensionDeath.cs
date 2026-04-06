using DG.Tweening;
using UnityEngine;

public class AngelAscensionDeath : PatientDeathBase
{
    [Header("Phase 0: Light Beam")]
    [Tooltip("빛 기둥 파티클. 환자 위치에 미리 배치, Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _lightBeam;

    [Header("Phase 0: Magic Circle")]
    [Tooltip("마법진 오브젝트. 자식 파티클 포함. 환자 발밑 바닥에 배치, Play On Awake 끄기.")]
    [SerializeField] private GameObject _magicCircle;

    [Header("Phase 1: Halo")]
    [Tooltip("천사 고리 오브젝트. 환자 머리 위에 배치, 비활성.")]
    [SerializeField] private Transform _halo;

    [Tooltip("천사 고리 등장 시점 (초).")]
    [SerializeField] private float _haloAppearTime = 0.3f;

    [Tooltip("천사 고리 스케일 애니메이션 시간 (초).")]
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

    [Tooltip("날개 펄럭임 스케일 배율 (1 기준 확대량).")]
    [SerializeField] private float _flapScaleAmount = 0.15f;

    [Tooltip("한 번 펄럭이는 시간 (초).")]
    [SerializeField] private float _flapDuration = 0.4f;

    [Header("Phase 4: Ascension")]
    [Tooltip("상승 시작 시점 (초).")]
    [SerializeField] private float _ascensionStartTime = 1.0f;

    [Tooltip("상승 높이 (미터).")]
    [SerializeField] private float _ascensionHeight = 8f;

    [Tooltip("상승 시간 (초).")]
    [SerializeField] private float _ascensionDuration = 2.0f;

    [Header("VFX: Sparkle Trail")]
    [Tooltip("상승 중 반짝이 파티클. 환자 자식으로 배치, Play On Awake 끄기.")]
    [SerializeField] private ParticleSystem _sparkleFx;

    [Header("Phase 0: Fade In")]
    [Tooltip("빛 기둥/마법진 등장 시간 (초).")]
    [SerializeField] private float _beamFadeInDuration = 0.5f;

    [Header("Phase 5: Fade Out")]
    [Tooltip("빛 기둥/마법진 소멸 시점 (초).")]
    [SerializeField] private float _beamFadeTime = 3.0f;

    [Tooltip("빛 기둥/마법진 페이드 시간 (초).")]
    [SerializeField] private float _beamFadeDuration = 0.5f;

    private Sequence _sequence;
    private Tween _flapTween;
    private Transform _originalPatientParent;
    private Vector3 _originalPatientScale;

    // Inspector에서 설정한 원본 스케일 캐싱.
    private Vector3 _originalHaloScale;
    private Vector3 _originalWingsScale;
    private Vector3 _originalLightBeamScale;
    private Vector3 _originalMagicCircleScale;
    private bool _scalesCached;

    private void CacheOriginalScales()
    {
        if (_scalesCached)
        {
            return;
        }

        _scalesCached = true;

        if (_halo != null)
        {
            _originalHaloScale = _halo.localScale;
        }

        if (_wings != null)
        {
            _originalWingsScale = _wings.localScale;
        }

        if (_lightBeam != null)
        {
            _originalLightBeamScale = _lightBeam.transform.localScale;
        }

        if (_magicCircle != null)
        {
            _originalMagicCircleScale = _magicCircle.transform.localScale;
        }
    }

    public override Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform)
    {
        CacheOriginalScales();
        ForceComplete(patientRoot, bedTransform, patientTransform);

        _originalPatientParent = patientTransform.parent;
        _originalPatientScale = patientTransform.localScale;

        // 천사 고리, 날개 초기 상태: 스케일 0 (원본 비율 유지하면서 팝인).
        if (_halo != null)
        {
            _halo.localScale = Vector3.zero;
        }

        if (_wings != null)
        {
            _wings.localScale = Vector3.zero;
        }

        // Phase 0: 빛 기둥 + 마법진 활성화 및 페이드인.
        if (_lightBeam != null)
        {
            _lightBeam.transform.localScale = Vector3.zero;
            _lightBeam.gameObject.SetActive(true);
            PlayFx(_lightBeam);
            _lightBeam.transform.DOScale(_originalLightBeamScale, _beamFadeInDuration)
                .SetEase(Ease.OutQuad);
        }

        if (_magicCircle != null)
        {
            _magicCircle.transform.localScale = Vector3.zero;
            _magicCircle.SetActive(true);
            PlayAllFx(_magicCircle);
            _magicCircle.transform.DOScale(_originalMagicCircleScale, _beamFadeInDuration)
                .SetEase(Ease.OutQuad);
        }

        _sequence = DOTween.Sequence();

        // Phase 1: 천사 고리 팝인 (원본 스케일로 복원).
        _sequence.InsertCallback(_haloAppearTime, () =>
        {
            if (_halo != null)
            {
                _halo.gameObject.SetActive(true);
                _halo.DOScale(_originalHaloScale, _haloScaleDuration).SetEase(Ease.OutBack);
            }
        });

        // Phase 2: 날개 팝인 (원본 스케일로 복원).
        _sequence.InsertCallback(_wingsAppearTime, () =>
        {
            if (_wings != null)
            {
                _wings.gameObject.SetActive(true);
                _wings.DOScale(_originalWingsScale, _wingsScaleDuration).SetEase(Ease.OutBack);
            }
        });

        // Phase 3: 날개 펄럭임 (스케일 펄스 — 양쪽 날개 모델 대응).
        _sequence.InsertCallback(_flapStartTime, () =>
        {
            if (_wings != null)
            {
                Vector3 flapTarget = _originalWingsScale * (1f + _flapScaleAmount);
                _flapTween = _wings
                    .DOScale(flapTarget, _flapDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        });

        // Phase 4: 월드 Y축으로 상승 (스케일 변경 없음).
        _sequence.InsertCallback(_ascensionStartTime, () =>
        {
            patientTransform.SetParent(null, worldPositionStays: true);
            PlayFx(_sparkleFx);

            Vector3 ascensionTarget = patientTransform.position + new Vector3(0f, _ascensionHeight, 0f);
            patientTransform.DOMove(ascensionTarget, _ascensionDuration)
                .SetEase(Ease.InQuad);
        });

        // Phase 5: 빛 기둥 + 마법진 페이드.
        _sequence.InsertCallback(_beamFadeTime, () =>
        {
            if (_lightBeam != null)
            {
                _lightBeam.transform
                    .DOScale(Vector3.zero, _beamFadeDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        ClearFx(_lightBeam);
                        _lightBeam.gameObject.SetActive(false);
                    });
            }

            if (_magicCircle != null)
            {
                _magicCircle.transform
                    .DOScale(Vector3.zero, _beamFadeDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        ClearAllFx(_magicCircle);
                        _magicCircle.SetActive(false);
                    });
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
            _halo.localScale = _scalesCached ? _originalHaloScale : _halo.localScale;
        }

        if (_wings != null)
        {
            _wings.DOKill();
            _wings.localScale = _scalesCached ? _originalWingsScale : _wings.localScale;
        }

        if (_lightBeam != null)
        {
            _lightBeam.transform.DOKill();
            ClearFx(_lightBeam);
            if (_scalesCached)
            {
                _lightBeam.transform.localScale = _originalLightBeamScale;
            }
        }

        if (_magicCircle != null)
        {
            _magicCircle.transform.DOKill();
            ClearAllFx(_magicCircle);
            if (_scalesCached)
            {
                _magicCircle.transform.localScale = _originalMagicCircleScale;
            }
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
            _lightBeam.gameObject.SetActive(false);
        }

        if (_magicCircle != null)
        {
            _magicCircle.SetActive(false);
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
