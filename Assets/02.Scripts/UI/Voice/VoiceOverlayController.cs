using Photon.Realtime;
using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(VoiceOverlayView))]
public class VoiceOverlayController : MonoBehaviour
{
    [SerializeField] private VoiceOverlayView _view;

    private PhotonVoiceManager _voiceManager;
    private PortraitManager _portraitManager;
    private SceneLoadManager _sceneLoadManager;
    private readonly Player[] _overlayPlayers = new Player[4];
    private readonly int[] _slotActorNumbers = new int[4];
    private readonly string[] _slotNicknames = new string[4];
    private readonly Sprite[] _slotIconSprites = new Sprite[4];
    private readonly Color[] _slotTextColors = new Color[4];
    private bool _isOverlayVisible;
    private int _displayedPlayerCount;

    private void Awake()
    {
        _view ??= GetComponent<VoiceOverlayView>();
        ClearSlotAssignments();
    }

    private void OnEnable()
    {
        BindSceneLoadManager();
        BindManagers();
        RefreshOverlay();
    }

    private void Start()
    {
        BindSceneLoadManager();
        BindManagers();
        RefreshOverlay();
    }

    private void OnDisable()
    {
        UnbindSceneLoadManager();
        UnbindManagers();
    }

    private void HandleSceneLoadComplete(ESceneType _)
    {
        BindManagers();
        RefreshOverlay();
    }

    private void HandleOverlayStateChanged()
    {
        RefreshOverlay();
    }

    private void HandlePortraitStateChanged()
    {
        RefreshOverlay();
    }

    private void BindManagers()
    {
        BindVoiceManager();
        BindPortraitManager();
    }

    private void BindVoiceManager()
    {
        PhotonVoiceManager manager = PhotonVoiceManager.Instance;
        if (_voiceManager == manager)
        {
            return;
        }

        if (_voiceManager != null)
        {
            _voiceManager.OverlayStateChanged -= HandleOverlayStateChanged;
        }

        _voiceManager = manager;

        if (_voiceManager != null)
        {
            _voiceManager.OverlayStateChanged += HandleOverlayStateChanged;
        }
    }

    private void BindPortraitManager()
    {
        PortraitManager manager = PortraitManager.Instance;
        if (_portraitManager == manager)
        {
            return;
        }

        if (_portraitManager != null)
        {
            _portraitManager.PortraitStateChanged -= HandlePortraitStateChanged;
        }

        _portraitManager = manager;

        if (_portraitManager != null)
        {
            _portraitManager.PortraitStateChanged += HandlePortraitStateChanged;
        }
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

    private void UnbindManagers()
    {
        if (_voiceManager != null)
        {
            _voiceManager.OverlayStateChanged -= HandleOverlayStateChanged;
            _voiceManager = null;
        }

        if (_portraitManager != null)
        {
            _portraitManager.PortraitStateChanged -= HandlePortraitStateChanged;
            _portraitManager = null;
        }
    }

    private void RefreshOverlay()
    {
        if (_view == null)
        {
            return;
        }

        PhotonVoiceManager voiceManager = PhotonVoiceManager.Instance;
        if (HasOverlayStructureChanged(voiceManager))
        {
            RebuildSlots();
            return;
        }

        if (HasOverlayTextColorChanged(voiceManager))
        {
            RefreshSlotTextColors();
        }

        RefreshVoiceState();
    }

    private void RebuildSlots()
    {
        if (_view == null)
        {
            return;
        }

        PhotonVoiceManager voiceManager = PhotonVoiceManager.Instance;
        if (voiceManager == null)
        {
            _view.SetVisible(false);
            _view.HideAll();
            _isOverlayVisible = false;
            _displayedPlayerCount = 0;
            ClearSlotAssignments();
            return;
        }

        _view.Initialize(voiceManager.OverlaySlotCount);

        bool shouldDisplay = voiceManager.ShouldDisplayOverlay();
        _view.SetVisible(shouldDisplay);
        _isOverlayVisible = shouldDisplay;
        if (!shouldDisplay)
        {
            _view.HideAll();
            _displayedPlayerCount = 0;
            ClearSlotAssignments();
            return;
        }

        int playerCount = voiceManager.FillOverlayPlayers(_overlayPlayers);
        _displayedPlayerCount = playerCount;
        for (int i = 0; i < voiceManager.OverlaySlotCount; i++)
        {
            if (i >= playerCount)
            {
                _view.HideSlot(i);
                ClearSlotAssignment(i);
                continue;
            }

            Player player = _overlayPlayers[i];
            string nickname = PlayerProperty.GetNickname(player);
            Color textColor = voiceManager.GetOverlayTextColor(player);

            Sprite iconSprite = voiceManager.SlotIconSprite;
            if (_portraitManager != null && _portraitManager.TryGetPortrait(player, out Sprite portraitSprite))
            {
                iconSprite = portraitSprite;
            }

            bool identityChanged = _slotActorNumbers[i] != player.ActorNumber
                || !string.Equals(_slotNicknames[i], nickname, StringComparison.Ordinal)
                || _slotIconSprites[i] != iconSprite;
            bool textColorChanged = _slotTextColors[i] != textColor;

            _slotActorNumbers[i] = player.ActorNumber;
            _slotNicknames[i] = nickname;
            _slotIconSprites[i] = iconSprite;
            _slotTextColors[i] = textColor;

            if (identityChanged)
            {
                _view.SetSlotIdentityContent(i, nickname, iconSprite);
            }

            if (textColorChanged || identityChanged)
            {
                _view.SetSlotTextColor(i, textColor);
            }
        }

        RefreshVoiceState();
    }

    private void RefreshSlotTextColors()
    {
        if (_view == null || !_isOverlayVisible)
        {
            return;
        }

        PhotonVoiceManager voiceManager = PhotonVoiceManager.Instance;
        if (voiceManager == null || !voiceManager.ShouldDisplayOverlay())
        {
            RebuildSlots();
            return;
        }

        int playerCount = voiceManager.FillOverlayPlayers(_overlayPlayers);
        if (playerCount != _displayedPlayerCount)
        {
            RebuildSlots();
            return;
        }

        for (int i = 0; i < playerCount; i++)
        {
            Player player = _overlayPlayers[i];
            if (player == null || _slotActorNumbers[i] != player.ActorNumber)
            {
                RebuildSlots();
                return;
            }

            Color textColor = voiceManager.GetOverlayTextColor(player);
            if (_slotTextColors[i] == textColor)
            {
                continue;
            }

            _slotTextColors[i] = textColor;
            _view.SetSlotTextColor(i, textColor);
        }
    }

    private void RefreshVoiceState()
    {
        if (_view == null || !_isOverlayVisible)
        {
            return;
        }

        PhotonVoiceManager voiceManager = PhotonVoiceManager.Instance;
        if (voiceManager == null || !voiceManager.ShouldDisplayOverlay())
        {
            RebuildSlots();
            return;
        }

        int playerCount = voiceManager.FillOverlayPlayers(_overlayPlayers);
        if (playerCount != _displayedPlayerCount)
        {
            RebuildSlots();
            return;
        }

        for (int i = 0; i < playerCount; i++)
        {
            Player player = _overlayPlayers[i];
            if (player == null
                || _slotActorNumbers[i] != player.ActorNumber
                || !string.Equals(_slotNicknames[i], PlayerProperty.GetNickname(player), StringComparison.Ordinal)
                || _slotIconSprites[i] != ResolveIconSprite(player, voiceManager)
                || _slotTextColors[i] != voiceManager.GetOverlayTextColor(player))
            {
                RebuildSlots();
                return;
            }

            _view.SetSlotVoiceState(
                i,
                PlayerProperty.GetVoiceSpeaking(player),
                PlayerProperty.GetVoiceMuted(player));
        }
    }

    private bool HasOverlayStructureChanged(PhotonVoiceManager voiceManager)
    {
        if (voiceManager == null)
        {
            return _isOverlayVisible || _displayedPlayerCount > 0;
        }

        bool shouldDisplay = voiceManager.ShouldDisplayOverlay();
        if (shouldDisplay != _isOverlayVisible)
        {
            return true;
        }

        if (!shouldDisplay)
        {
            return false;
        }

        int playerCount = voiceManager.FillOverlayPlayers(_overlayPlayers);
        if (playerCount != _displayedPlayerCount)
        {
            return true;
        }

        for (int i = 0; i < playerCount; i++)
        {
            Player player = _overlayPlayers[i];
            if (player == null
                || _slotActorNumbers[i] != player.ActorNumber
                || !string.Equals(_slotNicknames[i], PlayerProperty.GetNickname(player), StringComparison.Ordinal)
                || _slotIconSprites[i] != ResolveIconSprite(player, voiceManager))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasOverlayTextColorChanged(PhotonVoiceManager voiceManager)
    {
        if (voiceManager == null || !_isOverlayVisible)
        {
            return false;
        }

        int playerCount = voiceManager.FillOverlayPlayers(_overlayPlayers);
        if (playerCount != _displayedPlayerCount)
        {
            return true;
        }

        for (int i = 0; i < playerCount; i++)
        {
            Player player = _overlayPlayers[i];
            if (player == null || _slotTextColors[i] != voiceManager.GetOverlayTextColor(player))
            {
                return true;
            }
        }

        return false;
    }

    private Sprite ResolveIconSprite(Player player, PhotonVoiceManager voiceManager)
    {
        Sprite iconSprite = voiceManager.SlotIconSprite;
        if (_portraitManager != null && _portraitManager.TryGetPortrait(player, out Sprite portraitSprite))
        {
            iconSprite = portraitSprite;
        }

        return iconSprite;
    }

    private void ClearSlotAssignments()
    {
        for (int i = 0; i < _slotActorNumbers.Length; i++)
        {
            ClearSlotAssignment(i);
        }
    }

    private void ClearSlotAssignment(int index)
    {
        _slotActorNumbers[index] = -1;
        _slotNicknames[index] = null;
        _slotIconSprites[index] = null;
        _slotTextColors[index] = default;
    }
}
