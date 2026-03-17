using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class WaitingRoomPresenter
{
    private readonly WaitingRoomView _view;
    private readonly WaitingRoomModel _model;

    public WaitingRoomPresenter(WaitingRoomView view, WaitingRoomModel model)
    {
        _view = view;
        _model = model;

        PhotonServerManager.Instance.OnMasterClientChanged += OnMasterClientChange;
    }

    private void OnMasterClientChange()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SetIsMaster(true);
        }
        else
        {
            SetIsMaster(false);
        }
    }

    public void ReadyStateChange()
    {
        _model.ToggleReady();
        PlayerProperty.SetReadyState(_model.IsReady);
    }

    public void SetIsMaster(bool isMaster)
    {
        _model.SetIsMater(isMaster);

        Initialize();
    }

    public void GameStart()
    {
        Debug.Log("게임 시작");

        Player[] players = PhotonNetwork.PlayerList;

        foreach (Player player in players)
        {
            if (player.IsMasterClient) continue;
            if (PlayerProperty.GetReadyState(player) == false)
            {
                _view.ShowErrorMessage("모든 플레이어가 준비해야 합니다.");
                Debug.Log("모든 플레이어가 준비해야 합니다.");
                return;
            }
        }

        PhotonServerManager.Instance.StartStage();
        Debug.Log("게임 시작.");
    }

    public void Initialize()
    {
        if (_model.IsMaster)
        {
            _view.ShowMasterUI();
        }
        else
        {
            _view.ShowGuestUI();
            _view.ButtonSet(_model.IsReady);
        }
    }
}
