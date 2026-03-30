using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomPresenter
{
    private RoomView _view;
    private UIPopupBase _attendancePopup;


    public RoomPresenter(RoomView view, UIPopupBase attendancePopup)
    {
        _view = view;
        _attendancePopup = attendancePopup;

        PhotonServerManager.Instance.OnFailedToJoinRoom += OnFailedToJoinRoom;
        PlayerDataManager.Instance.OnDataManagerReady += OnDataManagerSet;
        SetDropdown();
    }

    public void EnterRoom(string code)
    {
        PhotonServerManager.Instance.TryJoinRoom(code);   
    }

    public void CreateRoom()
    {
        PhotonServerManager.Instance.CreateNewRoom();
    }

    public void SetNickName(string name)
    {
        PhotonServerManager.Instance.SetNickname(name);
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
    }

   
    public void SetDropdown()
    {
        if (!PlayerDataManager.Instance.IsReady) return;
        MyHospital[] hospitals = PlayerDataManager.Instance.GetHospital();

        _view.SetDropdown(hospitals);
    }


    public void OnMyHospitalDeleted(string code)
    {
        PlayerDataManager.Instance.DeleteHospital(code);
    }

    public void Dispose()
    {
        PhotonServerManager.Instance.OnFailedToJoinRoom -= OnFailedToJoinRoom;
        PlayerDataManager.Instance.OnDataManagerReady -= OnDataManagerSet;
    }
}
