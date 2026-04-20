using UnityEngine;
using TMPro;
using DG.Tweening;

public class UI_Respawn : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private GameObject _rootPanel;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _countdownText;

    [Header("애니메이션")]
    [SerializeField] private float _fadeDuration = 0.25f;

    [Header("텍스트 포맷")]
    [SerializeField] private string _countdownFormat = "{0}초 후 복귀";

    private Tween _fadeTween;

    private void Awake()
    {
        if (_rootPanel != null)
        {
            _rootPanel.SetActive(false);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
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
            _subscribedInstance.OnRespawnStarted += Show;
            _subscribedInstance.OnRespawnCountdown += SetCountdown;
            _subscribedInstance.OnRespawnEnded += Hide;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribedInstance != null)
        {
            _subscribedInstance.OnRespawnStarted -= Show;
            _subscribedInstance.OnRespawnCountdown -= SetCountdown;
            _subscribedInstance.OnRespawnEnded -= Hide;
            _subscribedInstance = null;
        }
    }

    public void Show()
    {
        if (_rootPanel == null) return;

        _rootPanel.SetActive(true);

        _fadeTween?.Kill();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _fadeTween = _canvasGroup
                .DOFade(1f, _fadeDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    public void Hide()
    {
        if (_rootPanel == null) return;

        _fadeTween?.Kill();

        if (_canvasGroup != null)
        {
            _fadeTween = _canvasGroup
                .DOFade(0f, _fadeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _rootPanel.SetActive(false));
        }
        else
        {
            _rootPanel.SetActive(false);
        }
    }

    public void SetCountdown(int seconds)
    {
        if (_countdownText == null) return;

        _countdownText.text = string.Format(_countdownFormat, seconds);
    }

    public void SetCountdown(float remainingTime)
    {
        int seconds = Mathf.CeilToInt(remainingTime);
        SetCountdown(seconds);
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
        _fadeTween = null;
    }
}
