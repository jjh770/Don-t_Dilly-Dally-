using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class RespawnScreenEffectController : MonoBehaviour
{
    [Header("Volume 설정")]
    [SerializeField] private Volume _volume;

    [Header("흑백 연출")]
    [SerializeField] private float _grayscaleSaturation = -100f;
    [SerializeField] private float _transitionDuration = 0.25f;

    private ColorAdjustments _colorAdjustments;
    private float _originalSaturation;
    private Tween _saturationTween;

    private void Awake()
    {
        if (_volume == null)
        {
            _volume = FindFirstObjectByType<Volume>();
        }

        if (_volume != null && _volume.profile.TryGet(out _colorAdjustments))
        {
            _originalSaturation = _colorAdjustments.saturation.value;
        }
    }

    private PlayerRespawnAbility _subscribedInstance;

    private void Update()
    {
        TrySubscribeToLocalPlayer();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribeToLocalPlayer()
    {
        var local = PlayerRespawnAbility.LocalInstance;
        if (local == _subscribedInstance) return;

        Unsubscribe();

        if (local != null)
        {
            _subscribedInstance = local;
            _subscribedInstance.OnRespawnStarted += EnableGrayscale;
            _subscribedInstance.OnRespawnEnded += DisableGrayscale;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribedInstance != null)
        {
            _subscribedInstance.OnRespawnStarted -= EnableGrayscale;
            _subscribedInstance.OnRespawnEnded -= DisableGrayscale;
            _subscribedInstance = null;
        }
    }

    public void EnableGrayscale()
    {
        if (_colorAdjustments == null) return;

        _saturationTween?.Kill();
        _saturationTween = DOTween.To(
            () => _colorAdjustments.saturation.value,
            x => _colorAdjustments.saturation.value = x,
            _grayscaleSaturation,
            _transitionDuration
        ).SetEase(Ease.OutQuad);
    }

    public void DisableGrayscale()
    {
        if (_colorAdjustments == null) return;

        _saturationTween?.Kill();
        _saturationTween = DOTween.To(
            () => _colorAdjustments.saturation.value,
            x => _colorAdjustments.saturation.value = x,
            _originalSaturation,
            _transitionDuration
        ).SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        _saturationTween?.Kill();
        _saturationTween = null;

        if (_colorAdjustments != null)
        {
            _colorAdjustments.saturation.value = _originalSaturation;
        }
    }
}
