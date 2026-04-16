using System.Collections;
using TMPro;
using UnityEngine;

public class UI_Commentary : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _narrationPanel;
    [SerializeField] private TextMeshProUGUI _narrationText;

    private CommentaryPlaybackManager _playbackManager;

    [Header("Settings")]
    [SerializeField] private float _fadeInDuration = 0.3f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private float _typingSpeed = 0.05f;

    [Header("Animation")]
    [SerializeField] private CanvasGroup _canvasGroup;

    private Coroutine _displayCoroutine;

    private bool _isSubscribed = false;

    private void Awake()
    {
        if (_canvasGroup == null && _narrationPanel != null)
        {
            _canvasGroup = _narrationPanel.GetComponent<CanvasGroup>();
        }

        HideImmediate();
    }


    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        if (_playbackManager == null && CommentaryController.Instance != null)
        {
            _playbackManager = CommentaryController.Instance.PlaybackManager;
        }

        if (_playbackManager != null)
        {
            _playbackManager.OnSubtitleChanged += OnSubtitleChanged;
            _isSubscribed = true;
        }
    }

    private void TryUnsubscribe()
    {
        if (!_isSubscribed) return;

        if (_playbackManager != null)
        {
            _playbackManager.OnSubtitleChanged -= OnSubtitleChanged;
        }
        _isSubscribed = false;
    }

    private void OnSubtitleChanged(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            HideWithFade();
        }
        else
        {
            ShowNarration(text);
        }
    }

    public void ShowNarration(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }

        _displayCoroutine = StartCoroutine(DisplayNarrationCoroutine(text));
    }

    private IEnumerator DisplayNarrationCoroutine(string text)
    {
        _narrationText.text = "";
        _narrationPanel.SetActive(true);

        // 페이드 인
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeInDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        // 타자 효과
        for (int i = 0; i < text.Length; i++)
        {
            _narrationText.text = text.Substring(0, i + 1);
            yield return new WaitForSeconds(_typingSpeed);
        }

        _displayCoroutine = null;
    }

    private void HideWithFade()
    {
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }
        _displayCoroutine = StartCoroutine(HideWithFadeCoroutine());
    }

    private IEnumerator HideWithFadeCoroutine()
    {
        // 페이드 아웃
        if (_canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeOutDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
        }

        _narrationPanel.SetActive(false);
        _displayCoroutine = null;
    }

    public void HideImmediate()
    {
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        if (_narrationPanel != null)
        {
            _narrationPanel.SetActive(false);
        }
    }
}
