using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaitingRoomView : MonoBehaviour
{
    [SerializeField] private Button _readyButton;
    [SerializeField] private TextMeshProUGUI _readyButtonText;
    [SerializeField] private string _readyText = "Ready";
    [SerializeField] private string _unreadyText = "Unready";

    private bool _isReady = false;

    private WaitingRoomPresenter _presenter;
    private void OnEnable()
    {
        _readyButton.onClick.AddListener(OnReadyButtonClicked);
    }

    private void OnReadyButtonClicked()
    {
        _isReady = !_isReady;
        ButtonSet(_isReady);

        _presenter.ReadyStateChange(_isReady);
    }

    private void ButtonSet(bool isReady)
    {
        _readyButtonText.text = isReady ? _unreadyText : _readyText;
    }

    public void Initialized(WaitingRoomPresenter presenter)
    {
        _presenter = presenter;
        ButtonSet(_isReady);
    }
}
