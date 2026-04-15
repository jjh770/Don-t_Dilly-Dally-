using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum ELoadingStep
{
    FirebaseInit,
    PlayerDataLoad,
    AttendanceLoad,
    NoInternet,
}

public static class LoadingUIEvents
{
    public static event Action<ELoadingStep> OnShowRequested;
    public static event Action OnHideRequested;

    public static bool IsVisible { get; private set; }
    public static ELoadingStep CurrentStep { get; private set; } = ELoadingStep.FirebaseInit;

    public static void Show(ELoadingStep step)
    {
        CurrentStep = step;
        IsVisible = true;
        OnShowRequested?.Invoke(step);
    }

    public static void Hide()
    {
        IsVisible = false;
        OnHideRequested?.Invoke();
    }
}

public class LoadingUI : PersistentSingleton<LoadingUI>
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _quitGameButton;

    private readonly Dictionary<ELoadingStep, string> _messages = new()
    {
        { ELoadingStep.FirebaseInit,    "서버에 연결하는 중..." },
        { ELoadingStep.PlayerDataLoad,  "플레이어 데이터를 불러오는 중..." },
        {ELoadingStep.NoInternet, "서버 연결에 실패하였습니다.\n연결 상태를 확인하여 재접속 해주세요." },
    };

    private void OnEnable()
    {
        LoadingUIEvents.OnShowRequested += Show;
        LoadingUIEvents.OnHideRequested += Hide;

        if (_quitGameButton != null)
        {
            _quitGameButton.onClick.AddListener(HandleQuitButtonClicked);
            _quitGameButton.gameObject.SetActive(false);
        }

        if (LoadingUIEvents.IsVisible)
        {
            Show(LoadingUIEvents.CurrentStep);
        }
        else
        {
            Hide();
        }
    }

    private void HandleQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        LoadingUIEvents.OnShowRequested -= Show;
        LoadingUIEvents.OnHideRequested -= Hide;
        if (_quitGameButton != null)
        {
            _quitGameButton.onClick.RemoveListener(HandleQuitButtonClicked);
        }
    }

    public void Show(ELoadingStep step)
    {
        if (_messages.TryGetValue(step, out string message))
        {
            _messageText.text = message;
        }
        if (step == ELoadingStep.NoInternet)
        {
            if (_quitGameButton != null) 
                _quitGameButton.gameObject.SetActive(true);
        }
        else
        {
            if (_quitGameButton != null)
                _quitGameButton.gameObject.SetActive(false);
        }
            _panel.SetActive(true);     
    }

    public void Hide() => _panel.SetActive(false);
}
