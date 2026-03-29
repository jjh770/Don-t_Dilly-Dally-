using UnityEngine;
using DG.Tweening;

public abstract class UIPopupBase : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] protected CanvasGroup _canvasGroup;
    [SerializeField] protected RectTransform _panel;

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
    }

    private void ShowImmediate()
    {
        _canvasGroup.alpha = 1;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    public virtual void Show()
    {
        PlayShowAnimation();
    }

    public virtual void Hide()
    {
        PlayHideAnimation();
    }

    /// <summary>
    /// 자식에서 데이터 세팅용
    /// </summary>
    protected abstract void OnShow();

    #region Animation

    protected virtual void PlayShowAnimation()
    {
        _tween?.Kill();

        _canvasGroup.alpha = 0;
        _panel.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();

        seq.Append(_canvasGroup.DOFade(1, 0.2f));
        seq.Join(_panel.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(_panel.DOScale(1f, 0.1f));
        seq.OnComplete(() =>
        {
            ShowImmediate();
            OnShow();
        });
    }

    protected virtual void PlayHideAnimation()
    {
        _tween?.Kill();

        Sequence seq = DOTween.Sequence();

        seq.Append(_canvasGroup.DOFade(0, 0.15f));
        seq.Join(_panel.DOScale(0.8f, 0.15f));
        seq.OnComplete(() =>
        {
            HideImmediate();
        });
    }

    #endregion
}