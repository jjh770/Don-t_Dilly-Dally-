using UnityEngine;
using DG.Tweening;
using System;

public abstract class UIPopupBase : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] protected CanvasGroup _canvasGroup;
    [SerializeField] protected RectTransform _panel;

    [Header("Animation")]
    [SerializeField] private float _fadeDuration = 0.2f;
    [SerializeField] private float _scaleDuration = 0.2f;
    [SerializeField] private float _overshootScale = 1.1f;
    [SerializeField] private float _settleDuration = 0.1f;
    [SerializeField] private Ease _scaleEase = Ease.OutBack;
    [SerializeField] private float _hideEndScale = 0.8f;

    protected Tween _tween;

    protected virtual void Awake()
    {
        HideImmediate();
    }

    private void HideImmediate()
    {
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _panel.gameObject.SetActive(false);
    }

    private void ShowImmediate()
    {
        _canvasGroup.alpha = 1;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _panel.gameObject.SetActive(true);
    }

    public virtual void Show() => PlayShowAnimation();
    public virtual void Show(Action onComplete = null) => PlayShowAnimation(onComplete);
    public virtual void Hide() => PlayHideAnimation();

    protected abstract void OnShow();

    #region Animation

    protected virtual void PlayShowAnimation(Action onComplete = null)
    {
        _tween?.Kill();
        _canvasGroup.alpha = 0;
        _panel.localScale = Vector3.zero;
        _panel.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();
        seq.Append(_canvasGroup.DOFade(1f, _fadeDuration));
        seq.Join(_panel.DOScale(_overshootScale, _scaleDuration).SetEase(_scaleEase));
        seq.Append(_panel.DOScale(1f, _settleDuration));
        seq.OnComplete(() =>
        {
            ShowImmediate();
            OnShow();
            onComplete?.Invoke();
        });
    }

    protected virtual void PlayHideAnimation()
    {
        _tween?.Kill();

        Sequence seq = DOTween.Sequence();
        seq.Append(_canvasGroup.DOFade(0f, _fadeDuration));
        seq.Join(_panel.DOScale(_hideEndScale, _scaleDuration));
        seq.OnComplete(HideImmediate);
    }

    #endregion
}