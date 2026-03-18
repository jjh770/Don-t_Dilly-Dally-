using Photon.Realtime;
using UnityEngine;

public class WaitingRoomPresenter
{
    private readonly WaitingRoomView _waitingRoomView;
    private readonly ContextMenuView _contextMenuView;
    private readonly WaitingRoomModel _model;

    public bool IsMaster => _model != null? _model.IsMaster : false;
    public WaitingRoomPresenter(WaitingRoomView waitingRoomView, ContextMenuView contextMenuView, WaitingRoomModel model)
    {
        _waitingRoomView = waitingRoomView;
        _contextMenuView = contextMenuView;
        _model = model;

        PhotonServerManager.Instance.OnMasterClientChanged += HandleMasterClientChanged;
    }

    public void Initialize()
    {
        RefreshWaitingRoomUI();
    }

    public void ToggleReadyState()
    {
        _model.ToggleReady();
        PlayerProperty.SetReadyState(_model.IsReady);
        RefreshWaitingRoomUI();
    }

    public void GameStart()
    {
        if (!PhotonServerManager.Instance.TryStartStage(out string errorMessage))
        {
            _waitingRoomView.ShowErrorMessage(errorMessage);
        }
    }

    public void CopyRoomCode()
    {
        string roomCode = PhotonServerManager.Instance.RoomCode;
        GUIUtility.systemCopyBuffer = roomCode;
    }

    public void ExitRoom()
    {
        PhotonServerManager.Instance.LeaveRoom();
    }

    public void SelectPlayer(Player targetPlayer, Vector3 position)
    {
        _model.SetSelectedPlayer(targetPlayer);
        _contextMenuView.Show(position);
    }

    public void ClearSelectedPlayer()
    {
        _model.SetSelectedPlayer(null);
        _contextMenuView.Hide();
    }

    public void KickSelectedPlayer()
    {
        if (_model.SelectedPlayer == null)
        {
            return;
        }

        PhotonServerManager.Instance.Kick(_model.SelectedPlayer);

        ClearSelectedPlayer();
    }

    public void GiveMasterToSelectedPlayer()
    {
        if (_model.SelectedPlayer == null)
        {
            return;
        }

        PhotonServerManager.Instance.ChangeMaster(_model.SelectedPlayer);
        
        ClearSelectedPlayer();
    }

    private void HandleMasterClientChanged()
    {
        UpdateMasterState(PhotonServerManager.Instance.IsMasterClient);
    }

    private void UpdateMasterState(bool isMaster)
    {
        _model.SetIsMaster(isMaster);
        RefreshWaitingRoomUI();
    }

    private void RefreshWaitingRoomUI()
    {
        _waitingRoomView.SetRoomCode(PhotonServerManager.Instance.RoomCode);

        if (_model.IsMaster)
        {
            _waitingRoomView.ShowMasterUI();
            return;
        }

        _waitingRoomView.ShowGuestUI();
        _waitingRoomView.ButtonSet(_model.IsReady);
    }
}
