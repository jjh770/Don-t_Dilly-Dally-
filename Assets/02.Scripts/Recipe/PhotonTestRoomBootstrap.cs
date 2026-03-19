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
            Debug.Log("[PhotonTestRoomBootstrap] Already connected.");
            TryJoinRoom();
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.GameVersion = _gameVersion;

        Debug.Log("[PhotonTestRoomBootstrap] ConnectUsingSettings()");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PhotonTestRoomBootstrap] OnConnectedToMaster");
        TryJoinRoom();
    }

    private void TryJoinRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[PhotonTestRoomBootstrap] Already in room.");
            return;
        }

        Debug.Log($"[PhotonTestRoomBootstrap] JoinOrCreateRoom: {_roomName}");
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = _maxPlayers
        };

        PhotonNetwork.JoinOrCreateRoom(_roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(
            $"[PhotonTestRoomBootstrap] OnJoinedRoom. " +
            $"Room={PhotonNetwork.CurrentRoom.Name}, " +
            $"PlayerCount={PhotonNetwork.CurrentRoom.PlayerCount}, " +
            $"IsMaster={PhotonNetwork.IsMasterClient}");

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
            Debug.LogWarning("[PhotonTestRoomBootstrap] Player prefab is not assigned.");
            return;
        }

        Vector3 spawnPosition = _player.transform.position;
        Quaternion spawnRotation = _player.transform.rotation;

        Debug.Log(
            $"[PhotonTestRoomBootstrap] Spawning test player. " +
            $"Prefab={_player.name}, Position={spawnPosition}, Rotation={spawnRotation.eulerAngles}");

        _spawnedPlayer = PhotonNetwork.Instantiate("Player", spawnPosition, spawnRotation);
        _hasSpawnedLocalPlayer = _spawnedPlayer != null;

        Debug.Log(
            $"[PhotonTestRoomBootstrap] Test player spawned. " +
            $"Success={_hasSpawnedLocalPlayer}, Name={_spawnedPlayer?.name}");
    }
}
