using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using UnityEngine;


public class PhotonServerManager : PunPersistentSingleton<PhotonServerManager>, IOnEventCallback
{

    [SerializeField]
    private int _roomIdLength = 6;

    [SerializeField]
    private int _maxPlayersPerRoom = 4;

    private const byte KickEventCode = 1;

    private readonly string _gameVersion = "1.0";

    private string _roomCode;

    private readonly System.Random _random = new System.Random();

    public bool IsMasterClient => PhotonNetwork.IsMasterClient;
    public bool GetLocalPlayerReadyState() => PlayerProperty.GetReadyState(PhotonNetwork.LocalPlayer);
    public string RoomCode => PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : null;
    public int CountOfPlayers => PhotonNetwork.CountOfPlayers;


    public event Action<string> OnFailedToJoinRoom;
    public event Action<Player, bool> OnReadyStateChanged;
    public event Action<Player, string> OnNicknameChanged;
    public event Action OnMasterClientChanged;

    private void Start()
    {
        Connect();
        PlayerDataManager.Instance.OnNicknameChanged += HandleNicknameChanged;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        PlayerDataManager.Instance.OnNicknameChanged -= HandleNicknameChanged;
    }
    private void Connect()
    {
        PhotonNetwork.GameVersion = _gameVersion;

        PhotonNetwork.EnableCloseConnection = true;
        PhotonNetwork.AutomaticallySyncScene = true;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PhotonServerManager] Connected to Master!");

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PhotonServerManager] Joined Lobby!");
    }

    public override void OnJoinedRoom()
    {
        _roomCode = null;
        SceneLoadManager.Instance.BeginSceneLoad(ESceneType.WaitingRoom);
        Debug.Log($"[PhotonServerManager] {PhotonNetwork.LocalPlayer.NickName} Joined room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"[PhotonServerManager] Joined room: {PhotonNetwork.CurrentRoom.PlayerCount}");

        PlayerProperty.EnsureProperties();
        RoomProperties.EnsureProperties();
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[PhotonServerManager] Failed to join room: {message}");

        switch (returnCode)
        {
            case ErrorCode.GameDoesNotExist:
                OnFailedToJoinRoom?.Invoke("방이 존재하지 않습니다.");
                break;
            case ErrorCode.GameClosed:
                OnFailedToJoinRoom?.Invoke("게임이 시작되었습니다.");
                break;
            case ErrorCode.GameFull:
                OnFailedToJoinRoom?.Invoke("방이 가득 찼습니다.");
                break;
            default:
                OnFailedToJoinRoom?.Invoke($"알 수 없는 오류: {message}");
                break;
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.TryGetValue(PlayerProperty.IsReadyKey, out object isReadyValue) && isReadyValue is bool isReady)
        {
            OnReadyStateChanged?.Invoke(targetPlayer, isReady);
        }
        if (changedProps.TryGetValue(PlayerProperty.NicknameKey, out object nicknameValue) && nicknameValue is string nickname)
        {
            OnNicknameChanged?.Invoke(targetPlayer, nickname);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        OnMasterClientChanged?.Invoke();
    }

    public override void OnLeftRoom()
    {
        SceneLoadManager.Instance.BeginSceneLoad(ESceneType.Lobby);
    }

    public void CreateNewRoom()
    {
        string roomCode = RandomString(_roomIdLength);
        OpenRoom(roomCode);
    }

    public void OpenRoom(string roomCode)
    {
        if (!CanAddRoom(roomCode)) return;

        SetNickname(PlayerDataManager.Instance.PlayerNickname);
        PhotonNetwork.CreateRoom(roomCode, GetRoomOptions());
    }

    public RoomOptions GetRoomOptions()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = _maxPlayersPerRoom;
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
        TryJoinRoomAsync(roomCode).Forget();
    }

    public async UniTask TryJoinRoomAsync(string roomCode)
    {
        if (await RoomDataManager.Instance.IsRoomDataExist(roomCode))
        {
            _roomCode = roomCode;

            if (!CanAddRoom(roomCode)) return;

            SetNickname(PlayerDataManager.Instance.PlayerNickname);
            PhotonNetwork.JoinOrCreateRoom(_roomCode, GetRoomOptions(), TypedLobby.Default);
        }
        else
        {
            OnFailedToJoinRoom?.Invoke("존재하지 않는 방입니다.");
            Debug.Log("[PhotonServerManager] 존재하지 않는 방입니다.");
        }
    }

    private bool CanAddRoom(string roomCode)
    {
        if (!PlayerDataManager.Instance.CanAddHospital(roomCode))
        {
            string errorMessage = "병원을 더이상 추가할 수 없습니다.";
            OnFailedToJoinRoom?.Invoke(errorMessage);
            Debug.Log("[PhotonServerManager] 병원을 더이상 추가할 수 없습니다.");
            return false;
        }

        return true;
    }

    public void HandleNicknameChanged(string nickname)
    {
        SetNickname(nickname);
    }

    public void SetNickname(string nickname)
    {
        PhotonNetwork.NickName = nickname;
        PlayerProperty.SetNickname(nickname);
    }

    public bool TryStartStage(out string message)
    {
        Player[] players = PhotonNetwork.PlayerList;

        foreach (Player player in players)
        {
            if (player.IsMasterClient) continue;
            if (PlayerProperty.GetReadyState(player) == false)
            {
                message = "모든 플레이어가 준비해야 합니다.";
                return false;
            }
        }
        PhotonNetwork.CurrentRoom.IsOpen = false;
        RoomProperties.SetGameInProgress(true);
        SceneLoadManager.Instance.BeginSceneLoad(ESceneType.Cutscene);
        message = string.Empty;
        return true;
    }

    public void ReturnWaitingRoom()
    {
        PhotonNetwork.CurrentRoom.IsOpen = true;
        RoomProperties.SetGameInProgress(false);
        SceneLoadManager.Instance.BeginSceneLoad(ESceneType.WaitingRoom);
    }

    public void ChangeMaster(Player player)
    {
        PhotonNetwork.SetMasterClient(player);
    }

    public void Kick(Player player)
    {
        object[] content = { "kicked" };

        RaiseEventOptions options = new RaiseEventOptions
        {
            TargetActors = new[] { player.ActorNumber }
        };

        SendOptions sendOptions = new SendOptions
        {
            Reliability = true
        };

        PhotonNetwork.RaiseEvent(KickEventCode, content, options, sendOptions);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == KickEventCode)
        {
            LeaveRoom();
        }
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            // PUN 방을 나가기 전에 보이스 클라이언트 연결을 수동으로 먼저 해제하여 FollowLeader 로직 차단
            if (PhotonVoiceManager.Instance != null && PhotonVoiceManager.Instance.VoiceConnection != null && PhotonVoiceManager.Instance.VoiceConnection.Client != null)
            {
                PhotonVoiceManager.Instance.VoiceConnection.Client.Disconnect();
            }

            PhotonNetwork.LeaveRoom();
        }
    }
}
