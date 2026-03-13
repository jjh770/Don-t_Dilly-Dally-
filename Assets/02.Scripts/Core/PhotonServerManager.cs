using System;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonServerManager : PunPersistentSingleton<PhotonServerManager>
{
 
    [SerializeField]
    private int _roomIdLength = 6;

    private readonly string _gameVersion = "1.0";

    private string _nickName = "Player";

    private string _roomCode;

    private readonly System.Random _random = new System.Random();

    public event Action<string> OnFailedToJoinRoom;

    private void Start()
    {
        Connect();
    }

    private void Connect()
    {
        PhotonNetwork.GameVersion = _gameVersion;
        PhotonNetwork.NickName = _nickName;

        PhotonNetwork.AutomaticallySyncScene = true;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master!");

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby!");
    }

    public override void OnJoinedRoom()
    {
        _roomCode = null;
        Debug.Log($"{PhotonNetwork.LocalPlayer.NickName} Joined room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to join room: {message}");

        switch (returnCode)
        {
            case ErrorCode.GameDoesNotExist:
                OnFailedToJoinRoom?.Invoke("방이 존재하지 않습니다.");
                break;
            case ErrorCode.GameFull:
                OnFailedToJoinRoom?.Invoke("방이 가득 찼습니다.");
                break;
            default:
                OnFailedToJoinRoom?.Invoke($"알 수 없는 오류: {message}");
                break;
        }
    }

    public void CreateNewRoom()
    {
        string roomName = RandomString(_roomIdLength);
        OpenRoom(roomName);
    }

    public void OpenRoom(string roomName)
    {
        PhotonNetwork.CreateRoom(roomName, GetRoomOptions());
    }

    public RoomOptions GetRoomOptions()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 1;   
        roomOptions.IsOpen = true; 
        roomOptions.IsVisible = true; 
        return roomOptions;
    }

    private string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public void TryJoinRoom(string roomCode)
    {
        // TODO : 데이터에 존재하는 방인지 체크
        _roomCode = roomCode;
        PhotonNetwork.JoinOrCreateRoom(_roomCode, GetRoomOptions(), TypedLobby.Default);
    }

    public void SetNickname(string nickname)
    {
        _nickName = nickname;
        PhotonNetwork.NickName = _nickName;
    }
}
