
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

public class RoomPresenter
{
    private RoomView _view;

    public RoomPresenter(RoomView view)
    {
        _view = view;
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

        string[] codes = hospitals.Select(hospital => $"{hospital.Name}").ToArray();
        string[] Dates = hospitals.Select(hospital => $"최근 접속 : {hospital.Time.ToLocalTime():yy.MM.dd HH:mm}").ToArray();

        _view.SetDropdown(codes, Dates);
    }

    public void Dispose()
    {
        PhotonServerManager.Instance.OnFailedToJoinRoom -= OnFailedToJoinRoom;
    }
}
