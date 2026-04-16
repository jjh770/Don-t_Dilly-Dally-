using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    private static LoadingUI _instance;

    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _quitGameButton;

    private readonly Dictionary<ELoadingStep, string> _messages = new()
    {
        { ELoadingStep.FirebaseInit,    "서버에 연결하는 중..." },
        { ELoadingStep.PlayerDataLoad,  "플레이어 데이터를 불러오는 중..." },
        { ELoadingStep.NoInternet, "서버 연결에 실패하였습니다.\n연결 상태를 확인하여 재접속 해주세요." },
    };

    private void OnEnable()
    {
        LoadingUIService.OnShowRequested += Show;
        LoadingUIService.OnHideRequested += Hide;

        if (_quitGameButton != null)
        {
            _quitGameButton.onClick.AddListener(HandleQuitButtonClicked);
            _quitGameButton.gameObject.SetActive(false);
        }

        if (LoadingUIService.IsVisible)
        {
            Show(LoadingUIService.CurrentStep);
        }
        else
        {
            Hide();
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
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
        LoadingUIService.OnShowRequested -= Show;
        LoadingUIService.OnHideRequested -= Hide;
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
