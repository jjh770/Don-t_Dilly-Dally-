using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaitingRoomView : MonoBehaviour
{
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _gameStartButton;
    [SerializeField] private TextMeshProUGUI _readyButtonText;
    [SerializeField] private string _readyText = "Ready";
    [SerializeField] private string _unreadyText = "Unready";

    private WaitingRoomPresenter _presenter;
    private void OnEnable()
    {
        _readyButton.onClick.AddListener(OnReadyButtonClicked);
        _gameStartButton.onClick.AddListener(OnGameStartButtonClicked);
    }

    private void OnReadyButtonClicked()
    {
        _presenter.ReadyStateChange();
    }

    private void OnGameStartButtonClicked()
    {
        _presenter.GameStart();
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

    public void OnDestroy()
    {
        _readyButton.onClick.RemoveListener(OnReadyButtonClicked);
        _gameStartButton.onClick.RemoveListener(OnGameStartButtonClicked);
    }
}
