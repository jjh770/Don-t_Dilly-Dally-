using System.Collections;
using TMPro;
using UnityEngine;

public class UI_Commentary : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _narrationPanel;
    [SerializeField] private TextMeshProUGUI _narrationText;

    [Header("Settings")]
    [SerializeField] private float _displayDuration = 4f;
    [SerializeField] private float _fadeInDuration = 0.3f;
    [SerializeField] private float _fadeOutDuration = 0.5f;

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

    private void Start()
    {
        // Start에서 다시 구독 시도 (초기화 순서 문제 해결)
        TrySubscribe();
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

        if (CommentaryController.Instance != null)
        {
            CommentaryController.Instance.OnNarrationGenerated += ShowNarration;
            _isSubscribed = true;
        }
        else
        {
            // Instance가 아직 없으면 다음 프레임에 재시도
            StartCoroutine(RetrySubscribe());
        }
    }

    private void TryUnsubscribe()
    {
        if (!_isSubscribed) return;

        if (CommentaryController.Instance != null)
        {
            CommentaryController.Instance.OnNarrationGenerated -= ShowNarration;
        }
        _isSubscribed = false;
    }

    private System.Collections.IEnumerator RetrySubscribe()
    {
        yield return null; // 다음 프레임 대기

        if (!_isSubscribed && CommentaryController.Instance != null)
        {
            CommentaryController.Instance.OnNarrationGenerated += ShowNarration;
            _isSubscribed = true;
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
        _narrationText.text = text;
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

        yield return new WaitForSeconds(_displayDuration);

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
