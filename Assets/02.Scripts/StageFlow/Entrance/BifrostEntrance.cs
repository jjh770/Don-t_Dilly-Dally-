using DG.Tweening;
using UnityEngine;
using UnityEngine.VFX;

public class BifrostEntrance : PatientEntranceBase
{
    [Header("Bifrost Effect")]
    [Tooltip("씬에 미리 배치된 Bifrost 이펙트 루트. 자식으로 VisualEffect(빔)과 ParticleSystem(빛)들을 가짐.")]
    [SerializeField] private GameObject _bifrostRoot;

    [Tooltip("Bifrost 이펙트 시작 시점 (초).")]
    [SerializeField] private float _effectStartTime = 0f;

    [Tooltip("Bifrost 이펙트 정지 시점 (초). 파티클 발생만 멈추고 기존 파티클은 서서히 사라짐.")]
    [SerializeField] private float _effectStopTime = 2.8f;

    [Header("Bed Appearance (침대 등장)")]
    [Tooltip("침대 스케일 팝업 시작 시점 (초). 빛줄기 최대 후 등장하도록 설정.")]
    [SerializeField] private float _bedAppearTime = 1.5f;

    [Tooltip("침대 스케일 0→1 전환 시간 (초).")]
    [SerializeField] private float _bedScaleDuration = 0.5f;

    [Tooltip("침대 스케일 팝업 커브. OutBack=뿅 하고 살짝 커졌다 줄어드는 느낌.")]
    [SerializeField] private Ease _bedScaleEase = Ease.OutBack;

    [Header("Sequence")]
    [Tooltip("전체 연출 최소 길이 (초). VFX 정리 시간 포함.")]
    [SerializeField] private float _totalDuration = 3.5f;

    private Sequence _sequence;
    private Vector3 _cachedBedScale;
    private VisualEffect[] _visualEffects;
    private ParticleSystem[] _particleSystems;

    private void Awake()
    {
        CacheEffectComponents();
        SetBifrostActive(false);
    }

    public override Sequence Play(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation)
    {
        ForceComplete(bedTransform, finalPosition, finalRotation);

        _cachedBedScale = bedTransform.localScale;

        bedTransform.position = finalPosition;
        bedTransform.rotation = finalRotation;
        bedTransform.localScale = Vector3.zero;

        SetBifrostActive(true);

        _sequence = DOTween.Sequence();

        // Bifrost effect play/stop.
        _sequence.InsertCallback(_effectStartTime, PlayEffects);
        _sequence.InsertCallback(_effectStopTime, StopEffects);

        // Bed scale pop-up.
        _sequence.Insert(
            _bedAppearTime,
            bedTransform.DOScale(_cachedBedScale, _bedScaleDuration).SetEase(_bedScaleEase));

        // Ensure minimum sequence length for VFX cleanup.
        float currentDuration = _sequence.Duration();
        if (currentDuration < _totalDuration)
        {
            _sequence.AppendInterval(_totalDuration - currentDuration);
        }

        // Hide bifrost after sequence completes.
        _sequence.OnComplete(() => SetBifrostActive(false));

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

        if (_cachedBedScale != Vector3.zero)
        {
            bedTransform.localScale = _cachedBedScale;
        }

        ClearEffects();
        SetBifrostActive(false);
    }

    // ── Cache ────────────────────────────────────────────────────

    private void CacheEffectComponents()
    {
        if (_bifrostRoot == null)
        {
            _visualEffects = System.Array.Empty<VisualEffect>();
            _particleSystems = System.Array.Empty<ParticleSystem>();
            return;
        }

        _visualEffects = _bifrostRoot.GetComponentsInChildren<VisualEffect>(true);
        _particleSystems = _bifrostRoot.GetComponentsInChildren<ParticleSystem>(true);
    }

    // ── Activate / Deactivate ────────────────────────────────────

    private void SetBifrostActive(bool active)
    {
        if (_bifrostRoot != null)
        {
            _bifrostRoot.SetActive(active);
        }
    }

    // ── Play ─────────────────────────────────────────────────────

    private void PlayEffects()
    {
        foreach (VisualEffect vfx in _visualEffects)
        {
            if (vfx != null)
            {
                vfx.Play();
            }
        }

        foreach (ParticleSystem ps in _particleSystems)
        {
            if (ps != null)
            {
                ps.Play();
            }
        }
    }

    // ── Stop (emit stops, existing particles fade naturally) ─────

    private void StopEffects()
    {
        foreach (VisualEffect vfx in _visualEffects)
        {
            if (vfx != null)
            {
                vfx.Stop();
            }
        }

        foreach (ParticleSystem ps in _particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    // ── Clear (immediate cleanup for animation restart) ──────────

    private void ClearEffects()
    {
        foreach (VisualEffect vfx in _visualEffects)
        {
            if (vfx != null)
            {
                vfx.Stop();
                vfx.Reinit();
            }
        }

        foreach (ParticleSystem ps in _particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
