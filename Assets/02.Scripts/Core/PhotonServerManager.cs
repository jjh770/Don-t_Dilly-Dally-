using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using DontDillyDally.StageFlow;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
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

    private bool _isEnabled = true;

    public bool IsMasterClient => PhotonNetwork.IsMasterClient;
    public bool GetLocalPlayerReadyState() => PlayerProperty.GetReadyState(PhotonNetwork.LocalPlayer);
    public string RoomCode => PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : null;

    public bool IsEnabled => _isEnabled;

    public event Action<string> OnFailedToJoinRoom;
    public event Action<Player, bool> OnReadyStateChanged;
    public event Action<Player, string> OnNicknameChanged;
    public event Action OnMasterClientChanged;
    public event Action OnOtherPlayerLeftRoom;
    private bool _isReturningToWaitingRoom;

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

    private void HandleConnectError(string log)
    {
        if (_isEnabled)
        {
            _isEnabled = false;
            Debug.LogError($"[PhotonServerManager] Connect Error: {log}");
            LoadingUIEvents.Show(ELoadingStep.NoInternet);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[PhotonServerManager] Disconnected: {cause}");

        switch (cause)
        {
            case DisconnectCause.DisconnectByClientLogic:
            case DisconnectCause.DisconnectByServerLogic:
                return; // 정상 종료로 간주

            case DisconnectCause.ExceptionOnConnect:
                HandleConnectError("네트워크 연결을 확인해주세요.");
                break;

            case DisconnectCause.ServerTimeout:
                HandleConnectError("서버 연결 시간이 초과되었습니다.");
                break;

            case DisconnectCause.ClientTimeout:
                HandleConnectError("네트워크가 불안정합니다.");
                break;

            default:
                HandleConnectError($"연결이 끊어졌습니다. ({cause})");
                break;
        }

        
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

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        OnOtherPlayerLeftRoom?.Invoke();
    }

    public void CreateNewRoom()
    {
        string roomCode = RandomString(_roomIdLength);
        OpenRoom(roomCode);
    }

    private void OpenRoom(string roomCode)
    {
        if (!CanAddRoom(roomCode)) return;

        if (!CanProceedWithLobbyRequest()) return;

        SetNickname(PlayerDataManager.Instance.PlayerNickname);
        PhotonNetwork.CreateRoom(roomCode, GetRoomOptions());
    }

    private RoomOptions GetRoomOptions()
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
        if (!CanProceedWithLobbyRequest()) return;
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

   private bool CanProceedWithLobbyRequest()
    {
        Debug.Log($"[PhotonServerManager] Current Network State: {PhotonNetwork.NetworkClientState}");
        if (PhotonNetwork.NetworkClientState == ClientState.ConnectingToGameServer)
        {
            return false;
        }
        else if (PhotonNetwork.NetworkClientState != ClientState.JoinedLobby && PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
        {
            OnFailedToJoinRoom?.Invoke("연결 상태를 확인해주세요.");
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

    public bool TryGetPlayerByActorNumber(int actorNumber, out Player player)
    {
        player = null;

        if (!PhotonNetwork.InRoom ||
            PhotonNetwork.CurrentRoom == null ||
            PhotonNetwork.CurrentRoom.Players == null ||
            actorNumber <= 0)
        {
            return false;
        }

        return PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumber, out player);
    }

    public bool TryStartStage(out string message)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
        {
            message = "방장만 스테이지를 시작할 수 있습니다.";
            return false;
        }

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
        RoomProperties.SetStageDataPrepComplete(false);
        SceneLoadManager.Instance.BeginSceneLoad(ESceneType.Cutscene);
        message = string.Empty;
        return true;
    }

    public void ReturnWaitingRoom()
    {
        if (_isReturningToWaitingRoom)
        {
            return;
        }

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[PhotonServerManager] 대기실 복귀는 마스터 클라이언트의 씬 동기화를 기다립니다.");
            return;
        }

        ReturnWaitingRoomAsync().Forget();
    }

    private async UniTaskVoid ReturnWaitingRoomAsync()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        _isReturningToWaitingRoom = true;

        try
        {
            // cleanup보다 먼저 메시지 큐를 끈다.
            // 비마스터가 마스터보다 먼저 대기실에 도착해 Instantiate 이벤트를 보내면
            // 마스터가 게임씬에서 원격 플레이어를 생성한 뒤 LoadLevel의 캐시 재처리로
            // 동일 플레이어가 중복 생성되는 문제를 방지한다.
            PhotonNetwork.IsMessageQueueRunning = false;

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;
                RoomProperties.SetGameInProgress(false);
                RoomProperties.SetStageDataPrepComplete(false);

                int cleanedCount = CleanupGameplayPlayerObjects();
                cleanedCount += CleanupGameplayRoomObjects();
                if (StageFlowBootstrapper.Instance != null &&
                    StageFlowBootstrapper.Instance.CleanupSpawnedStageInstance())
                {
                    cleanedCount++;
                }

                if (cleanedCount > 0)
                {
                    Debug.Log($"[PhotonServerManager] 대기실 복귀 전 게임 오브젝트 {cleanedCount}개를 정리했습니다.");
                    await UniTask.DelayFrame(1);
                }
            }

            // 씬 전환 전 로컬 플레이어를 PhotonNetwork.Destroy로 정리하여
            // 서버의 버퍼된 인스턴스화 캐시를 제거합니다.
            PlayerSpawnManager.Instance?.DestroyLocalPlayer();
            SceneLoadManager.Instance.OnSceneLoadComplete += HandleWaitingRoomSceneLoaded;
            SceneLoadManager.Instance.BeginSceneLoad(ESceneType.WaitingRoom);

            // BeginSceneLoad가 이미 로딩 중이거나 씬 데이터 누락으로 시작되지 못하면
            // 메시지 큐와 플래그를 즉시 복구합니다.
            if (!SceneLoadManager.Instance.IsLoading)
            {
                SceneLoadManager.Instance.OnSceneLoadComplete -= HandleWaitingRoomSceneLoaded;
                PhotonNetwork.IsMessageQueueRunning = true;
                _isReturningToWaitingRoom = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonServerManager] 대기실 복귀 중 오류 발생: {e.Message}");
            SceneLoadManager.Instance.OnSceneLoadComplete -= HandleWaitingRoomSceneLoaded;
            PhotonNetwork.IsMessageQueueRunning = true;
            _isReturningToWaitingRoom = false;
        }
    }

    private void HandleWaitingRoomSceneLoaded(ESceneType sceneType)
    {
        SceneLoadManager.Instance.OnSceneLoadComplete -= HandleWaitingRoomSceneLoaded;
        PlayerRegistry.Clear();
        PhotonNetwork.IsMessageQueueRunning = true;
        _isReturningToWaitingRoom = false;
    }

    private static int CleanupGameplayRoomObjects()
    {
        int destroyedCount = 0;
        HashSet<GameObject> destroyed = new HashSet<GameObject>();

        // 1) Photon에 등록된 네트워크 오브젝트 정리
        // PhotonViewCollection은 Photon 내부 목록을 사용하므로 씬 전체 탐색이 불필요합니다.
        List<PhotonView> registeredViews = new List<PhotonView>();
        foreach (PhotonView photonView in PhotonNetwork.PhotonViewCollection)
        {
            registeredViews.Add(photonView);
        }

        foreach (PhotonView photonView in registeredViews)
        {
            if (photonView == null || photonView.gameObject == null)
                continue;

            if (!photonView.IsRoomView)
                continue;

            GameObject target = photonView.gameObject;
            if (!destroyed.Add(target))
                continue;

            if (!IsGameplayObject(target))
                continue;

            PhotonNetwork.Destroy(target);
            destroyedCount++;
        }

        // 2) Photon에 미등록된 scene 오브젝트 정리 (ViewID 0 등)
        PhotonView[] sceneViews = FindObjectsOfType<PhotonView>(true);
        foreach (PhotonView photonView in sceneViews)
        {
            if (photonView == null || photonView.gameObject == null)
                continue;

            if (!photonView.IsRoomView)
                continue;

            GameObject target = photonView.gameObject;
            if (!destroyed.Add(target))
                continue;

            if (!IsGameplayObject(target))
                continue;

            UnityEngine.Object.Destroy(target);
            destroyedCount++;
        }

        return destroyedCount;
    }

    private static int CleanupGameplayPlayerObjects()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            return 0;
        }

        HashSet<GameObject> playerObjects = new HashSet<GameObject>();
        PlayerController[] playerControllers = FindObjectsOfType<PlayerController>(true);
        foreach (PlayerController playerController in playerControllers)
        {
            if (playerController == null || playerController.PhotonView == null)
            {
                continue;
            }

            if (playerController.PhotonView.Owner != null)
            {
                playerObjects.Add(playerController.gameObject);
            }
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            PhotonNetwork.DestroyPlayerObjects(player);
        }

        foreach (GameObject playerObject in playerObjects)
        {
            if (playerObject != null)
            {
                UnityEngine.Object.Destroy(playerObject);
            }
        }

        return playerObjects.Count;
    }

    private static bool IsGameplayObject(GameObject target)
    {
        return target.GetComponent<ItemObject>() != null ||
               target.GetComponent<BasicMaterialSource>() != null ||
               target.GetComponent<MixToolSource>() != null ||
               target.GetComponent<TraySource>() != null ||
               target.GetComponent<StageFlowManager>() != null;
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
