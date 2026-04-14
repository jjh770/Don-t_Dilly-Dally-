using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
public enum ELoadingStep
{
    FirebaseInit,
    PlayerDataLoad,
    AttendanceLoad,
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

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _messageText;

    private readonly Dictionary<ELoadingStep, string> _messages = new()
    {
        { ELoadingStep.FirebaseInit,    "서버에 연결하는 중..." },
        { ELoadingStep.PlayerDataLoad,  "플레이어 데이터를 불러오는 중..." },
    };

    private void OnEnable()
    {
        LoadingUIEvents.OnShowRequested += Show;
        LoadingUIEvents.OnHideRequested += Hide;

        if (LoadingUIEvents.IsVisible)
        {
            Show(LoadingUIEvents.CurrentStep);
        }
        else
        {
            Hide();
        }
    }

    private void OnDisable()
    {
        LoadingUIEvents.OnShowRequested -= Show;
        LoadingUIEvents.OnHideRequested -= Hide;
    }

    public void Show(ELoadingStep step)
    {
        if (_messages.TryGetValue(step, out string message))
        {
            _messageText.text = message;
        }
        _panel.SetActive(true);
    }

    public void Hide() => _panel.SetActive(false);
}
