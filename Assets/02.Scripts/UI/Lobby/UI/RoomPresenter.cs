using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomPresenter
{
    private RoomView _view;
    private UIPopupBase _attendancePopup;
    private readonly PhotonServerManager _photonServerManager;
    private readonly PlayerDataManager _playerDataManager;


    public RoomPresenter(RoomView view, UIPopupBase attendancePopup)
    {
        _view = view;
        _attendancePopup = attendancePopup;
        _photonServerManager = PhotonServerManager.Instance;
        _playerDataManager = PlayerDataManager.Instance;

        if (_photonServerManager != null)
        {
            _photonServerManager.OnFailedToJoinRoom += OnFailedToJoinRoom;
        }

        if (_playerDataManager != null)
        {
            _playerDataManager.OnDataManagerReady += OnDataManagerSet;
        }

        if (_playerDataManager != null && _playerDataManager.IsReady)
        {
            OnDataManagerSet();
        }
    }

    public void EnterRoom(string code)
    {
        _photonServerManager?.TryJoinRoom(code);
    }

    public void CreateRoom()
    {
        _photonServerManager?.CreateNewRoom();
    }

    public void SetNickName(string name)
    {
        _playerDataManager?.ChangeNickname(name);
    }

    public void SelectMyHospital(string code)
    {
        _view.SetCodeInputField(code);
    }

    public void OnFailedToJoinRoom(string message)
    {
        _view?.ShowErrorMessage(message);
    }

    public void OnDataManagerSet()
    {
        SetDropdown();
        if (_playerDataManager != null)
        {
            _view.InitializeNicknameField(_playerDataManager.PlayerNickname);
        }
    }

   
    public void SetDropdown()
    {
        if (_playerDataManager == null || !_playerDataManager.IsReady) return;
        MyHospital[] hospitals = _playerDataManager.GetHospital();

        _view.SetDropdown(hospitals);
    }


    public void OnMyHospitalDeleted(string code)
    {
        _playerDataManager?.DeleteHospital(code);
    }

    public void Dispose()
    {
        if (_photonServerManager != null)
        {
            _photonServerManager.OnFailedToJoinRoom -= OnFailedToJoinRoom;
        }

        if (_playerDataManager != null)
        {
            _playerDataManager.OnDataManagerReady -= OnDataManagerSet;
        }
    }
}
