using Photon.Realtime;
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

    private void Awake()
    {
        _view ??= GetComponent<VoiceOverlayView>();
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

    private void HandleSceneLoadComplete(string sceneName)
    {
        BindManagers();
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
            return;
        }

        _view.Initialize(voiceManager.OverlaySlotCount, voiceManager.SlotIconSprite);

        bool shouldDisplay = voiceManager.ShouldDisplayOverlay();
        _view.SetVisible(shouldDisplay);
        if (!shouldDisplay)
        {
            _view.HideAll();
            return;
        }

        int playerCount = voiceManager.FillOverlayPlayers(_overlayPlayers);
        for (int i = 0; i < voiceManager.OverlaySlotCount; i++)
        {
            if (i >= playerCount)
            {
                _view.HideSlot(i);
                continue;
            }

            Player player = _overlayPlayers[i];

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
                PlayerProperty.GetVoiceMuted(player),
                iconSprite);
        }
    }
}
