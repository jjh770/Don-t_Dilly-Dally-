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
    [SerializeField] private TextMeshProUGUI _errorMessageText;
    [SerializeField] private string _readyText = "Ready";
    [SerializeField] private string _unreadyText = "Unready";
    [SerializeField] private float _errorFadeDuration = 0.25f;
    [SerializeField] private float _errorVisibleDuration = 1.5f;

    private WaitingRoomPresenter _presenter;
    private Tween _errorTween;

    private void Awake()
    {
        SetErrorAlpha(0f);
    }

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

    public void ShowErrorMessage(string message)
    {
        if (_errorMessageText == null) return;

        _errorTween?.Kill();
        _errorMessageText.text = message;
        SetErrorAlpha(0f);

        _errorTween = DOTween.Sequence()
            .Append(_errorMessageText.DOFade(1f, _errorFadeDuration))
            .AppendInterval(_errorVisibleDuration)
            .Append(_errorMessageText.DOFade(0f, _errorFadeDuration));
    }

    private void SetErrorAlpha(float alpha)
    {
        if (_errorMessageText == null) return;

        Color color = _errorMessageText.color;
        color.a = alpha;
        _errorMessageText.color = color;
    }

    public void OnDisable()
    {
        _errorTween?.Kill();
        _readyButton.onClick.RemoveListener(OnReadyButtonClicked);
        _gameStartButton.onClick.RemoveListener(OnGameStartButtonClicked);
        _exitButton.onClick.RemoveListener(OnExitRoomButtonClicked);
        _roomCodeCopyButton.onClick.RemoveListener(OnCopyButtonClicked);
    }
}
