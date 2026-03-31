using System;
using UnityEngine;
using UnityEngine.UI;

public class ContextMenuView : MonoBehaviour
{
    [SerializeField] private GameObject _playerPopupPanel;
    [SerializeField] private Button _giveMasterButton;
    [SerializeField] private Button _kickButton;

    private WaitingRoomPresenter _presenter;

    private void Awake()
    {
        Hide();
    }

    private void OnEnable()
    {
        _giveMasterButton.onClick.AddListener(OnGiveMasterButtonClicked);
        _kickButton.onClick.AddListener(OnKickButtonClicked);
    }

    public void Show(Vector3 position)
    {
        _playerPopupPanel.transform.position = position;
        _playerPopupPanel.SetActive(true);
    }

    public void Hide()
    {
        _playerPopupPanel.SetActive(false);
        _playerPopupPanel.transform.position = Vector3.zero;
    }

    public void SetPresenter(WaitingRoomPresenter presenter)
    {
        _presenter = presenter;
    }

    private void OnGiveMasterButtonClicked()
    {
        _presenter.GiveMasterToSelectedPlayer();
    }

    private void OnKickButtonClicked()
    {
        _presenter.KickSelectedPlayer();
    }

    private void OnDisable()
    {
        _giveMasterButton.onClick.RemoveListener(OnGiveMasterButtonClicked);
        _kickButton.onClick.RemoveListener(OnKickButtonClicked);
    }
}
