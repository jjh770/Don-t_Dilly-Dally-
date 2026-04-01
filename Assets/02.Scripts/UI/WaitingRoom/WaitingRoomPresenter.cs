using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class WaitingRoomPresenter
{
    private readonly WaitingRoomView _waitingRoomView;
    private readonly ContextMenuView _contextMenuView;
    private readonly WaitingRoomModel _model;
    private bool _wasMaster;

    public bool IsMaster => PhotonServerManager.Instance != null && PhotonServerManager.Instance.IsMasterClient;

    public WaitingRoomPresenter(WaitingRoomView waitingRoomView, ContextMenuView contextMenuView, WaitingRoomModel model)
    {
        _waitingRoomView = waitingRoomView;
        _contextMenuView = contextMenuView;
        _model = model;
        _wasMaster = PhotonServerManager.Instance != null && PhotonServerManager.Instance.IsMasterClient;

        PhotonServerManager.Instance.OnMasterClientChanged += HandleMasterClientChanged;
        PhotonServerManager.Instance.OnReadyStateChanged += HandleReadyStateChanged;
        RoomDataManager.Instance.OnRoomDataChanged += HandleRoomDataChanged;
    }

    private void HandleRoomDataChanged(int coin, int star)
    {
        _waitingRoomView.SetRoomCurrency(coin, star);
    }

    public void Initialize()
    {
        RefreshWaitingRoomUI();
    }

    public void ToggleReadyState()
    {
        bool nextReadyState = !PhotonServerManager.Instance.GetLocalPlayerReadyState();
        PlayerProperty.SetReadyState(nextReadyState);
        RefreshWaitingRoomUI();
    }

    public void GameStart()
    {
        if (!PhotonServerManager.Instance.TryStartStage(out string errorMessage))
        {
            _waitingRoomView.ShowMessage(errorMessage);
        }
    }

    public void CopyRoomCode()
    {
        string roomCode = PhotonServerManager.Instance.RoomCode;
        GUIUtility.systemCopyBuffer = roomCode;
        string message = "클립보드에 복사되었습니다.";
        _waitingRoomView.ShowMessage(message);
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
        bool isMaster = PhotonServerManager.Instance.IsMasterClient;

        if (_wasMaster && !isMaster)
        {
            PlayerProperty.SetReadyState(false);
        }

        _wasMaster = isMaster;
        RefreshWaitingRoomUI();
    }

    private void HandleReadyStateChanged(Player targetPlayer, bool isReady)
    {
        if (PhotonNetwork.LocalPlayer == null || targetPlayer == null) return;
        if (targetPlayer.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber) return;

        RefreshWaitingRoomUI();
    }

    private void RefreshWaitingRoomUI()
    {
        _waitingRoomView.SetRoomCode(PhotonServerManager.Instance.RoomCode);

        if (PhotonServerManager.Instance.IsMasterClient)
        {
            _waitingRoomView.ShowMasterUI();
            return;
        }

        _waitingRoomView.ShowGuestUI();
        _waitingRoomView.ButtonSet(PhotonServerManager.Instance.GetLocalPlayerReadyState());
    }

    public void Dispose()
    {
        if (PhotonServerManager.Instance != null)
        {
            PhotonServerManager.Instance.OnMasterClientChanged -= HandleMasterClientChanged;
            PhotonServerManager.Instance.OnReadyStateChanged -= HandleReadyStateChanged;
        }

        if (RoomDataManager.Instance != null)
        {
            RoomDataManager.Instance.OnRoomDataChanged -= HandleRoomDataChanged;
        }
    }
}
