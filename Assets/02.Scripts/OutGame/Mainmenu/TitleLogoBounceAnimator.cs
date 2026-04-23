using DG.Tweening;
using UnityEngine;

public class TitleLogoBounceAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform _target;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Float")]
    [SerializeField] private float _duration = 1.4f;
    [SerializeField] private float _moveY = 8f;
    [SerializeField] private float _scaleMultiplier = 1.04f;
    [SerializeField, Range(0f, 1f)] private float _minimumAlpha = 0.78f;
    [SerializeField] private Ease _ease = Ease.InOutSine;

    private Sequence _sequence;
    private Vector2 _originalAnchoredPosition;
    private Vector3 _originalScale;
    private float _originalAlpha = 1f;

    private void Awake()
    {
        if (_target == null)
        {
            _target = GetComponent<RectTransform>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        CaptureOriginalTransform();
    }

    private void OnEnable()
    {
        CaptureOriginalTransform();

        if (_playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (_target == null)
        {
            return;
        }

        Stop();
        _target.anchoredPosition = _originalAnchoredPosition;
        _target.localScale = _originalScale;
        SetAlpha(_originalAlpha);

        Vector2 floatPosition = _originalAnchoredPosition + new Vector2(0f, _moveY);
        Vector3 targetScale = _originalScale * _scaleMultiplier;
        float targetAlpha = Mathf.Clamp01(_minimumAlpha);

        _sequence = DOTween.Sequence()
            .SetUpdate(_useUnscaledTime)
            .Append(_target.DOAnchorPos(floatPosition, _duration).SetEase(_ease))
            .Join(_target.DOScale(targetScale, _duration).SetEase(_ease))
            .Join(DOTween.To(GetAlpha, SetAlpha, targetAlpha, _duration).SetEase(_ease))
            .Append(_target.DOAnchorPos(_originalAnchoredPosition, _duration).SetEase(_ease))
            .Join(_target.DOScale(_originalScale, _duration).SetEase(_ease))
            .Join(DOTween.To(GetAlpha, SetAlpha, _originalAlpha, _duration).SetEase(_ease))
            .SetLoops(-1, LoopType.Restart);
    }

    public void Stop()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }

        if (_target == null)
        {
            return;
        }

        _target.anchoredPosition = _originalAnchoredPosition;
        _target.localScale = _originalScale;
        SetAlpha(_originalAlpha);
    }

    private void CaptureOriginalTransform()
    {
        if (_target == null)
        {
            return;
        }

        _originalAnchoredPosition = _target.anchoredPosition;
        _originalScale = _target.localScale;
        _originalAlpha = GetAlpha();
    }

    private float GetAlpha()
    {
        return _canvasGroup != null ? _canvasGroup.alpha : 1f;
    }

    private void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = alpha;
        }
    }
}
