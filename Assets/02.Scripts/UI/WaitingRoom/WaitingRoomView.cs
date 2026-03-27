using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaitingRoomView : MonoBehaviour
{
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _gameStartButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _roomCodeCopyButton;

    [SerializeField] private TextMeshProUGUI _roomCodeText;
    [SerializeField] private TextMeshProUGUI _readyButtonText;
    [SerializeField] private UI_Message _errorMessage;
    [SerializeField] private string _readyText = "Ready";
    [SerializeField] private string _unreadyText = "Unready";
    

    private WaitingRoomPresenter _presenter;
    

    private void OnEnable()
    {
        _readyButton.onClick.AddListener(OnReadyButtonClicked);
        _gameStartButton.onClick.AddListener(OnGameStartButtonClicked);
        _exitButton.onClick.AddListener(OnExitRoomButtonClicked);
        _roomCodeCopyButton.onClick.AddListener(OnCopyButtonClicked);
    }

    private void OnCopyButtonClicked()
    {
        _presenter.CopyRoomCode();
    }

    private void OnReadyButtonClicked()
    {
        _presenter.ToggleReadyState();
    }

    private void OnGameStartButtonClicked()
    {
        _presenter.GameStart();
    }

    private void OnExitRoomButtonClicked()
    {
        _presenter.ExitRoom();
    }

    public void ButtonSet(bool isReady)
    {
        _readyButtonText.text = isReady ? _unreadyText : _readyText;
    }

    public void ShowMasterUI()
    {
        _readyButton.gameObject.SetActive(false);
        _gameStartButton.gameObject.SetActive(true);    
    }

    public void ShowGuestUI()
    {
        _readyButton.gameObject.SetActive(true);
        _gameStartButton.gameObject.SetActive(false);
    }

    public void Initialized(WaitingRoomPresenter presenter)
    {
        _presenter = presenter;
    }

    public void SetRoomCode(string roomCode)
    {
        _roomCodeText.text = roomCode;
    }

    public void ShowMessage(string message)
    {
        if (message == null) return;

        _errorMessage.Show(message);
    }

    public void OnDisable()
    {
        _readyButton.onClick.RemoveListener(OnReadyButtonClicked);
        _gameStartButton.onClick.RemoveListener(OnGameStartButtonClicked);
        _exitButton.onClick.RemoveListener(OnExitRoomButtonClicked);
        _roomCodeCopyButton.onClick.RemoveListener(OnCopyButtonClicked);
    }
}
