using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonServerManager : PunPersistentSingleton<PhotonServerManager>
{
 
    [SerializeField]
    private int roomIdLength = 6;

    private readonly string gameVersion = "1.0";

    private string _nickName = "Player";

    private void Start()
    {
        Connect();
    }

    private void Connect()
    {
        PhotonNetwork.GameVersion = gameVersion;
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
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to join room: {message}");
    }

    public void CreateNewRoom()
    {
        string roomName = RandomString(roomIdLength);
        OpenRoom(roomName);
    }

    public void OpenRoom(string roomName)
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;   
        roomOptions.IsOpen = true; 
        roomOptions.IsVisible = true; 

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    private string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public void TryJoinRoom(string roomCode)
    {
        if (!PhotonNetwork.JoinRoom(roomCode))
        {
            OpenRoom(roomCode);
        }
    }
}
