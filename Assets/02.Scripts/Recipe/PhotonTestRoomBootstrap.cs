using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonTestRoomBootstrap : MonoBehaviourPunCallbacks
{
    [SerializeField] private string _gameVersion = "test_001";
    [SerializeField] private string _roomName = "TestRoom";
    [SerializeField] private byte _maxPlayers = 4;
    [SerializeField] private GameObject _player;

    private GameObject _spawnedPlayer;
    private bool _hasSpawnedLocalPlayer;

    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            TryJoinRoom();
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.GameVersion = _gameVersion;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        TryJoinRoom();
    }

    private void TryJoinRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            return;
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = _maxPlayers
        };

        PhotonNetwork.JoinOrCreateRoom(_roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        TrySpawnLocalPlayer();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning(
            $"[PhotonTestRoomBootstrap] OnJoinRoomFailed. " +
            $"Code={returnCode}, Message={message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[PhotonTestRoomBootstrap] OnDisconnected: {cause}");
    }

    private void TrySpawnLocalPlayer()
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (_hasSpawnedLocalPlayer)
            return;

        if (_player == null)
        {
            return;
        }

        Vector3 spawnPosition = _player.transform.position;
        Quaternion spawnRotation = _player.transform.rotation;

        _spawnedPlayer = PhotonNetwork.Instantiate("Player", spawnPosition, spawnRotation);
        _hasSpawnedLocalPlayer = _spawnedPlayer != null;
    }
}
