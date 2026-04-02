using TMPro;
using UnityEngine;

// 컷씬 스킵 안내 UI
public class CutsceneSkipUI : MonoBehaviour
{
    private enum ESkipNoticeMode
    {
        Hidden = 0,
        Loading,
        SkipConfirm
    }

    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private string _skipConfirmMessage = "한 번 더 누르면 스킵됩니다";
    [SerializeField] private string _loadingMessage = "데이터 로딩 중입니다";

    private ESkipNoticeMode _currentMode = ESkipNoticeMode.Hidden;

    public bool IsVisible => _panel != null && _panel.activeSelf;
    public bool IsShowingLoading => _currentMode == ESkipNoticeMode.Loading;
    public bool IsShowingSkipConfirm => _currentMode == ESkipNoticeMode.SkipConfirm;

    private void Awake()
    {
        EnsureReferences();

        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    public void ShowSkipConfirm()
    {
        _currentMode = ESkipNoticeMode.SkipConfirm;
        ShowMessage(_skipConfirmMessage);
    }

    public void ShowLoading()
    {
        _currentMode = ESkipNoticeMode.Loading;
        ShowMessage(_loadingMessage);
    }

    private void ShowMessage(string message)
    {
        EnsureReferences();

        if (_messageText != null)
        {
            _messageText.text = message;
        }

        if (_panel != null)
        {
            _panel.SetActive(true);
        }
    }

    public void Hide()
    {
        _currentMode = ESkipNoticeMode.Hidden;

        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    private void EnsureReferences()
    {
        if (_panel == null)
        {
            _panel = gameObject;
        }

        if (_messageText == null)
        {
            _messageText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
