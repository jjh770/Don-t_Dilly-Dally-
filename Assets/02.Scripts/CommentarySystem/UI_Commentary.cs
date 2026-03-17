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
        if (CommentaryManager.Instance != null)
        {
            CommentaryManager.Instance.OnNarrationGenerated += ShowNarration;
        }
    }

    private void OnDisable()
    {
        if (CommentaryManager.Instance != null)
        {
            CommentaryManager.Instance.OnNarrationGenerated -= ShowNarration;
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
