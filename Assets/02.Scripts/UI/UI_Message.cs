using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UI_Message : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _errorMessageText;
    [SerializeField] private float _errorFadeDuration = 0.25f;
    [SerializeField] private float _errorVisibleDuration = 1.5f;

    [SerializeField] private RectTransform _panel;

    private CanvasGroup _canvasGroup;

    private Tween _errorTween;
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        SetErrorAlpha(0f);
    }

    private void Start()
    {
        if (_panel == null)
        {
            _panel = GetComponent<RectTransform>();
        }
    }

    private void SetText(string message)
    {
        _errorMessageText.text = $"{message}";
    }

    public void Show(string message)
    {
        SetText(message);

        _errorTween?.Kill();
        SetErrorAlpha(0f);

        _errorTween = DOTween.Sequence()
            .AppendCallback(() => LayoutRebuilder.ForceRebuildLayoutImmediate(_panel))
            .AppendInterval(0f)
            .AppendCallback(() => LayoutRebuilder.ForceRebuildLayoutImmediate(_panel))
            .Append(_canvasGroup.DOFade(1f, _errorFadeDuration))
            .AppendInterval(_errorVisibleDuration)
            .Append(_canvasGroup.DOFade(0f, _errorFadeDuration));
    }
    private void SetErrorAlpha(float alpha)
    {
        _canvasGroup.alpha = alpha;
    }

    private void OnDisable()
    {
        _errorTween?.Kill();
    }

}
