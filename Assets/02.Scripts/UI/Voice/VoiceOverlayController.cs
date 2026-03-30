using Photon.Realtime;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(VoiceOverlayView))]
public class VoiceOverlayController : MonoBehaviour
{
    [SerializeField] private VoiceOverlayView _view;

    private PhotonVoiceManager _voiceManager;
    private PortraitManager _portraitManager;
    private readonly int[] _slotActorNumbers = new int[4];

    private void Awake()
    {
        _view = GetComponent<VoiceOverlayView>();
        ResetSlotActors();
    }

    private void OnEnable()
    {
        BindManagers();
        RefreshOverlay();
    }

    private void Update()
    {
        if (_voiceManager != PhotonVoiceManager.Instance || _portraitManager != PortraitManager.Instance)
        {
            BindManagers();
            RefreshOverlay();
        }
    }

    private void OnDisable()
    {
        UnbindManagers();
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
            _voiceManager.OverlayStateChanged -= RefreshOverlay;
        }

        _voiceManager = manager;

        if (_voiceManager != null)
        {
            _voiceManager.OverlayStateChanged += RefreshOverlay;
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
            _portraitManager.PortraitStateChanged -= RefreshOverlay;
        }

        _portraitManager = manager;

        if (_portraitManager != null)
        {
            _portraitManager.PortraitStateChanged += RefreshOverlay;
        }
    }

    private void UnbindManagers()
    {
        if (_voiceManager != null)
        {
            _voiceManager.OverlayStateChanged -= RefreshOverlay;
            _voiceManager = null;
        }

        if (_portraitManager != null)
        {
            _portraitManager.PortraitStateChanged -= RefreshOverlay;
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
        if (voiceManager == null)
        {
            _view.SetVisible(false);
            _view.HideAll();
            ResetSlotActors();
            return;
        }

        _view.Initialize(voiceManager.OverlaySlotCount, voiceManager.SlotIconSprite);

        bool shouldDisplay = voiceManager.ShouldDisplayOverlay();
        _view.SetVisible(shouldDisplay);
        if (!shouldDisplay)
        {
            _view.HideAll();
            ResetSlotActors();
            return;
        }

        Player[] players = voiceManager.GetOverlayPlayers();
        for (int i = 0; i < voiceManager.OverlaySlotCount; i++)
        {
            if (i >= players.Length)
            {
                _slotActorNumbers[i] = -1;
                _view.HideSlot(i);
                continue;
            }

            Player player = players[i];
            _slotActorNumbers[i] = player.ActorNumber;

            Sprite iconSprite = voiceManager.SlotIconSprite;
            if (_portraitManager != null && _portraitManager.TryGetPortrait(player, out Sprite portraitSprite))
            {
                iconSprite = portraitSprite;
            }

            _view.SetSlot(
                i,
                PlayerProperty.GetNickname(player),
                voiceManager.GetOverlayTextColor(player),
                PlayerProperty.GetVoiceSpeaking(player),
                iconSprite);
        }
    }

    private void ResetSlotActors()
    {
        for (int i = 0; i < _slotActorNumbers.Length; i++)
        {
            _slotActorNumbers[i] = -1;
        }
    }
}
