using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private bool _isShuttingDown;
    private SelectRoleManager _boundRoleManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheRecorder();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        CacheRecorder();
        BindRoleManager();
    }

    private void Start()
    {
        CacheRecorder();
        BindRoleManager();
        SyncLocalSpeakingState(forceUpdate: true);
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
        SyncLocalSpeakingState();
    }

    public override void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
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

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (!_isShuttingDown)
        {
            ResetLocalSpeakingState();
        }

        UnbindRoleManager();
        Instance = null;
    }

    public override void OnJoinedRoom()
    {
        _lastLocalSpeakingState = false;
        ResetLocalSpeakingState();
        SyncLocalSpeakingState(forceUpdate: true);
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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        BindRoleManager();
        NotifyOverlayStateChanged();
    }

    public bool ShouldDisplayOverlay()
    {
        if (!PhotonNetwork.InRoom)
        {
            return false;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName.IndexOf("Lobby", StringComparison.OrdinalIgnoreCase) < 0;
    }

    public bool ShouldUseWhiteOverlayText()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName.IndexOf("Game", StringComparison.OrdinalIgnoreCase) < 0;
    }

    public Player[] GetOverlayPlayers()
    {
        if (!PhotonNetwork.InRoom)
        {
            return Array.Empty<Player>();
        }

        return PhotonNetwork.PlayerList
            .OrderBy(player => player.IsMasterClient ? 0 : 1)
            .ThenBy(player => player.ActorNumber)
            .Take(MaxOverlaySlots)
            .ToArray();
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

    private void SyncLocalSpeakingState(bool forceUpdate = false)
    {
        if (PhotonNetwork.LocalPlayer == null || !PhotonNetwork.InRoom)
        {
            if (forceUpdate || _lastLocalSpeakingState)
            {
                ResetLocalSpeakingState();
            }

            return;
        }

        if (_recorder == null)
        {
            if (forceUpdate || _lastLocalSpeakingState)
            {
                ResetLocalSpeakingState();
                NotifyOverlayStateChanged();
            }

            return;
        }

        bool isSpeaking = _recorder.TransmitEnabled && _recorder.IsCurrentlyTransmitting;
        if (!forceUpdate && isSpeaking == _lastLocalSpeakingState)
        {
            return;
        }

        PlayerProperty.SetVoiceSpeaking(isSpeaking);
        _lastLocalSpeakingState = isSpeaking;
        NotifyOverlayStateChanged();
    }

    private void ResetLocalSpeakingState()
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            PlayerProperty.SetVoiceSpeaking(false);
        }

        _lastLocalSpeakingState = false;
    }

    private void NotifyOverlayStateChanged()
    {
        OverlayStateChanged?.Invoke();
    }
}
