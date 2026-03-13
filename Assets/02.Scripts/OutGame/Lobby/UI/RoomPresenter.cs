using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomPresenter
{
    private RoomView _view;

    public RoomPresenter(RoomView view)
    {
        view = _view;
    }
    public void EnterRoom(string code)
    {
        PhotonServerManager.Instance.TryJoinRoom(code);
    }

    public void CreateRoom()
    {
        PhotonServerManager.Instance.CreateNewRoom();
    }
}
