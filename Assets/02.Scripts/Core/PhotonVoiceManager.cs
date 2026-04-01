using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PhotonVoiceManager : MonoBehaviourPunCallbacks
{
    private const int MaxOverlaySlots = 4;
    public static PhotonVoiceManager Instance { get; private set; }

    [Header("Voice Overlay Visuals")]
    [SerializeField] private RoleVisualProfileSO _roleVisualProfile;
    [SerializeField] private Sprite _slotIconSprite;

    public event Action OverlayStateChanged;

    public int OverlaySlotCount => MaxOverlaySlots;
    public Sprite SlotIconSprite => _slotIconSprite;
    public Recorder Recorder => _recorder;

    private Recorder _recorder;
    private bool _lastLocalSpeakingState;
    private bool _lastLocalMutedState;
    private bool _isShuttingDown;
    private SelectRoleManager _boundRoleManager;
    private SceneLoadManager _sceneLoadManager;

    // 외부에 제공할 VoiceConnection 프로퍼티 추가
    [Header("Voice Connection")]
    [SerializeField] private VoiceConnection _voiceConnection;
    public VoiceConnection VoiceConnection => _voiceConnection;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheRecorder();

        if (_voiceConnection == null)
        {
            _voiceConnection = GetComponent<VoiceConnection>();
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        BindSceneLoadManager();
        CacheRecorder();
        BindRoleManager();
    }

    private void Start()
    {
        SyncLocalVoiceStates(forceUpdate: true);
        NotifyOverlayStateChanged();
    }

    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        CacheRecorder();
        BindRoleManager();
        SyncLocalVoiceStates();
    }

    public override void OnDisable()
    {
        UnbindSceneLoadManager();
        UnbindRoleManager();
        base.OnDisable();
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        UnbindSceneLoadManager();
        if (!_isShuttingDown)
        {
            ResetLocalSpeakingState();
        }

        UnbindRoleManager();
        Instance = null;
    }

    public override void OnJoinedRoom()
    {
        ResetLocalSpeakingState();
        SyncLocalVoiceStates(forceUpdate: true);
        NotifyOverlayStateChanged();
    }

    public override void OnLeftRoom()
    {
        ResetLocalSpeakingState();
        NotifyOverlayStateChanged();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        NotifyOverlayStateChanged();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        NotifyOverlayStateChanged();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        NotifyOverlayStateChanged();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        NotifyOverlayStateChanged();
    }

    private void HandleSceneLoadComplete(ESceneType _)
    {
        BindRoleManager();
        NotifyOverlayStateChanged();
    }

    private ESceneType GetCurrentSceneType()
    {
        return SceneLoadManager.Instance != null
            ? SceneLoadManager.Instance.CurrentSceneType
            : default;
    }

    public bool ShouldDisplayOverlay()
    {
        if (!PhotonNetwork.InRoom)
        {
            return false;
        }

        return GetCurrentSceneType() != ESceneType.Lobby;
    }

    public bool ShouldUseWhiteOverlayText()
    {
        return GetCurrentSceneType() != ESceneType.Gameplay;
    }

    public int FillOverlayPlayers(Player[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        int maxCount = Math.Min(buffer.Length, MaxOverlaySlots);
        if (!PhotonNetwork.InRoom || maxCount == 0)
        {
            return 0;
        }

        Room currentRoom = PhotonNetwork.CurrentRoom;
        if (currentRoom == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Player player in currentRoom.Players.Values)
        {
            if (player == null)
            {
                continue;
            }

            InsertOverlayPlayer(buffer, ref count, maxCount, player);
        }

        return count;
    }

    public Color GetOverlayTextColor(Player player)
    {
        if (ShouldUseWhiteOverlayText())
        {
            return Color.white;
        }

        RoleVisualProfileSO visualProfile = ResolveRoleVisualProfile();
        if (visualProfile == null)
        {
            return Color.white;
        }

        return visualProfile.GetNicknameColor(ResolveOverlayRole(player));
    }

    private RoleVisualProfileSO ResolveRoleVisualProfile()
    {
        if (_roleVisualProfile != null)
        {
            return _roleVisualProfile;
        }

        if (SelectRoleManager.Instance != null)
        {
            return SelectRoleManager.Instance.VisualProfile;
        }

        return null;
    }

    private RoleType ResolveOverlayRole(Player player)
    {
        BindRoleManager();

        if (_boundRoleManager != null && _boundRoleManager.TryGetAssignedRole(player, out RoleType assignedRole))
        {
            return assignedRole;
        }

        return RoleProperties.GetPlayerRole(player);
    }

    private static void InsertOverlayPlayer(Player[] buffer, ref int count, int maxCount, Player candidate)
    {
        int insertIndex = 0;
        while (insertIndex < count && CompareOverlayPlayers(buffer[insertIndex], candidate) <= 0)
        {
            insertIndex++;
        }

        if (insertIndex >= maxCount)
        {
            return;
        }

        if (count < maxCount)
        {
            count++;
        }

        for (int i = count - 1; i > insertIndex; i--)
        {
            buffer[i] = buffer[i - 1];
        }

        buffer[insertIndex] = candidate;
    }

    private static int CompareOverlayPlayers(Player left, Player right)
    {
        int leftPriority = left.IsMasterClient ? 0 : 1;
        int rightPriority = right.IsMasterClient ? 0 : 1;
        int priorityComparison = leftPriority.CompareTo(rightPriority);
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return left.ActorNumber.CompareTo(right.ActorNumber);
    }

    private void CacheRecorder()
    {
        if (_recorder != null)
        {
            return;
        }

        _recorder = GetComponent<Recorder>();
    }

    private void BindRoleManager()
    {
        SelectRoleManager manager = SelectRoleManager.Instance;
        if (_boundRoleManager == manager)
        {
            return;
        }

        UnbindRoleManager();
        _boundRoleManager = manager;

        if (_boundRoleManager == null)
        {
            return;
        }

        _boundRoleManager.OnPlayerRoleChanged += HandlePlayerRoleChanged;
        _boundRoleManager.OnRolesCleared += HandleRolesCleared;
    }

    private void BindSceneLoadManager()
    {
        SceneLoadManager manager = SceneLoadManager.Instance;
        if (_sceneLoadManager == manager)
        {
            return;
        }

        UnbindSceneLoadManager();
        _sceneLoadManager = manager;

        if (_sceneLoadManager != null)
        {
            _sceneLoadManager.OnSceneLoadComplete += HandleSceneLoadComplete;
        }
    }

    private void UnbindSceneLoadManager()
    {
        if (_sceneLoadManager == null)
        {
            return;
        }

        _sceneLoadManager.OnSceneLoadComplete -= HandleSceneLoadComplete;
        _sceneLoadManager = null;
    }

    private void UnbindRoleManager()
    {
        if (_boundRoleManager == null)
        {
            return;
        }

        _boundRoleManager.OnPlayerRoleChanged -= HandlePlayerRoleChanged;
        _boundRoleManager.OnRolesCleared -= HandleRolesCleared;
        _boundRoleManager = null;
    }

    private void HandlePlayerRoleChanged(int actorNumber, RoleType role)
    {
        NotifyOverlayStateChanged();
    }

    private void HandleRolesCleared()
    {
        NotifyOverlayStateChanged();
    }

    private void SyncLocalVoiceStates(bool forceUpdate = false)
    {
        if (PhotonNetwork.LocalPlayer == null || !PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState != ClientState.Joined)
        {
            // 방에 없을 때는 로컬 캐시 변수만 초기화하고 서버에 요청(SetProperties)을 보내지 않습니다.
            _lastLocalMutedState = false;
            _lastLocalSpeakingState = false;
            return;
        }

        if (_recorder == null)
        {
            if (forceUpdate || _lastLocalSpeakingState || _lastLocalMutedState)
            {
                ResetLocalSpeakingState();
                NotifyOverlayStateChanged();
            }

            return;
        }

        bool isMuted = !_recorder.TransmitEnabled;
        bool isSpeaking = _recorder.TransmitEnabled && _recorder.IsCurrentlyTransmitting;
        if (!forceUpdate && isSpeaking == _lastLocalSpeakingState && isMuted == _lastLocalMutedState)
        {
            return;
        }

        PlayerProperty.SetVoiceState(isMuted, isSpeaking);
        _lastLocalMutedState = isMuted;
        _lastLocalSpeakingState = isSpeaking;
        NotifyOverlayStateChanged();
    }

    private void ResetLocalSpeakingState()
    {
        // 로컬 변수 상태는 무조건 초기화
        _lastLocalMutedState = false;
        _lastLocalSpeakingState = false;

        // 핵심: 현재 방 안에 있고, 연결 상태가 완전히 Joined일 때만 서버에 프로퍼티를 동기화합니다.
        if (PhotonNetwork.InRoom && PhotonNetwork.NetworkClientState == ClientState.Joined)
        {
            if (PhotonNetwork.LocalPlayer != null)
            {
                PlayerProperty.SetVoiceState(false, false);
            }
        }
    }

    private void NotifyOverlayStateChanged()
    {
        OverlayStateChanged?.Invoke();
    }
}
