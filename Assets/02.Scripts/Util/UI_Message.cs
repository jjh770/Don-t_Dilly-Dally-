using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UI_Message : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _errorMessageText;
    [SerializeField] private float _errorFadeDuration = 0.25f;
    [SerializeField] private float _errorVisibleDuration = 1.5f;

    private CanvasGroup _canvasGroup;

    private Tween _errorTween;
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        SetErrorAlpha(0f);
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
