using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[DisallowMultipleComponent]
public class PortraitManager : PunPersistentSingleton<PortraitManager>
{
    [Header("Face Portrait")]
    [SerializeField] private GameObject _portraitCharacterPrefab;
    [SerializeField] private FacePortraitProfile _facePortraitProfile = new FacePortraitProfile();
    [SerializeField] private int _portraitResolution = 256;
    [SerializeField] private int _portraitLayer = 7;

    public event Action PortraitStateChanged;

    private CustomizingManager _boundCustomizingManager;
    private IPlayerAppearanceSource _appearanceSource;
    private PlayerPortraitService _portraitService;
    private CancellationTokenSource _portraitCts;

    public override void OnEnable()
    {
        base.OnEnable();
        BindCustomizingManager();
    }

    private void Start()
    {
        BindCustomizingManager();

        if (PhotonNetwork.InRoom)
        {
            PreloadAllPortraits().Forget();
        }
    }

    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        if (_boundCustomizingManager == null)
        {
            BindCustomizingManager();
        }
    }

    public override void OnDisable()
    {
        UnbindCustomizingManager();
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        UnbindCustomizingManager();
        DisposePortraitService();
    }

    public override void OnJoinedRoom()
    {
        PreloadAllPortraits().Forget();
        NotifyPortraitStateChanged();
    }

    public override void OnLeftRoom()
    {
        DisposePortraitService();
        NotifyPortraitStateChanged();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        PreloadPortrait(newPlayer).Forget();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        _portraitService?.Invalidate(otherPlayer.ActorNumber);
        NotifyPortraitStateChanged();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!CustomizingProperties.TryGetFromChangedProps(changedProps, out _))
        {
            return;
        }

        _portraitService?.Invalidate(targetPlayer.ActorNumber);
        PreloadPortrait(targetPlayer).Forget();
    }

    public bool TryGetPortrait(Player player, out Sprite portraitSprite)
    {
        portraitSprite = null;

        IPlayerPortraitService portraitService = GetOrCreatePortraitService();
        if (player == null || portraitService == null)
        {
            return false;
        }

        if (portraitService.TryGetCached(player.ActorNumber, out portraitSprite))
        {
            return true;
        }

        PlayerAppearanceSnapshot snapshot = CreateAppearanceSnapshot(player);
        if (snapshot == null)
        {
            return false;
        }

        return portraitService.TryGetCached(snapshot, out portraitSprite);
    }

    private void BindCustomizingManager()
    {
        CustomizingManager manager = CustomizingManager.Instance;
        if (_boundCustomizingManager == manager)
        {
            return;
        }

        UnbindCustomizingManager();
        _boundCustomizingManager = manager;

        if (_boundCustomizingManager == null)
        {
            return;
        }

        _boundCustomizingManager.OnSaved += HandleLocalAppearanceSaved;
    }

    private void UnbindCustomizingManager()
    {
        if (_boundCustomizingManager == null)
        {
            return;
        }

        _boundCustomizingManager.OnSaved -= HandleLocalAppearanceSaved;
        _boundCustomizingManager = null;
    }

    private void HandleLocalAppearanceSaved()
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        _portraitService?.Invalidate(PhotonNetwork.LocalPlayer.ActorNumber);
        PreloadPortrait(PhotonNetwork.LocalPlayer).Forget();
    }

    private PlayerAppearanceSnapshot CreateAppearanceSnapshot(Player player)
    {
        return GetOrCreateAppearanceSource()?.Create(player);
    }

    private IPlayerAppearanceSource GetOrCreateAppearanceSource()
    {
        if (_appearanceSource != null)
        {
            return _appearanceSource;
        }

        if (CustomizingManager.Instance == null)
        {
            return null;
        }

        _appearanceSource = new PhotonPlayerAppearanceSource(CustomizingManager.Instance);
        return _appearanceSource;
    }

    private IPlayerPortraitService GetOrCreatePortraitService()
    {
        if (_portraitService != null)
        {
            return _portraitService;
        }

        if (_portraitCharacterPrefab == null || CustomizingManager.Instance == null)
        {
            return null;
        }

        RuntimeFacePortraitRenderer renderer = new RuntimeFacePortraitRenderer(_portraitCharacterPrefab, CustomizingManager.Instance);
        _portraitService = new PlayerPortraitService(
            GetOrCreateAppearanceSource(),
            renderer,
            _facePortraitProfile,
            _portraitResolution,
            _portraitLayer);

        return _portraitService;
    }

    private void DisposePortraitService()
    {
        _portraitCts?.Cancel();
        _portraitCts?.Dispose();
        _portraitCts = null;

        if (_portraitService == null)
        {
            return;
        }

        _portraitService.ClearRoomCache();
        _portraitService = null;
        _appearanceSource = null;
    }

    private async UniTaskVoid PreloadAllPortraits()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            await PreloadPortrait(players[i]);
        }
    }

    private async UniTask PreloadPortrait(Player player)
    {
        if (player == null || !PhotonNetwork.InRoom)
        {
            return;
        }

        IPlayerPortraitService portraitService = GetOrCreatePortraitService();
        if (portraitService == null)
        {
            return;
        }

        PlayerAppearanceSnapshot snapshot = CreateAppearanceSnapshot(player);
        if (snapshot == null)
        {
            return;
        }

        _portraitCts ??= new CancellationTokenSource();

        Sprite portrait;
        try
        {
            portrait = await portraitService.GetOrCreateAsync(snapshot, _portraitCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (portrait == null)
        {
            return;
        }

        NotifyPortraitStateChanged();
    }

    private void NotifyPortraitStateChanged()
    {
        PortraitStateChanged?.Invoke();
    }
}
