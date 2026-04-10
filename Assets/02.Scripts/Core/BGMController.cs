using DontDillyDally.StageFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMController : PersistentSingleton<BGMController>
{
    private SceneLoadManager _sceneLoadManager;
    private BGMKey _currentKey = BGMKey.None;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        TryBindSceneLoadManager();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        StageFlowBootstrapper.StageFlowReady += HandleStageFlowReady;
        TryBindSceneLoadManager();
    }

    private void Start()
    {
        RefreshCurrentBgm();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;

        if (_sceneLoadManager != null)
        {
            _sceneLoadManager.OnSceneLoadComplete -= HandleSceneLoadComplete;
            _sceneLoadManager = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindSceneLoadManager();
        RefreshCurrentBgm();
    }

    private void HandleSceneLoadComplete(ESceneType sceneType)
    {
        ApplySceneBgm(sceneType);
    }

    private void HandleStageFlowReady()
    {
        ApplyStageBgm();
    }

    private void TryBindSceneLoadManager()
    {
        if (_sceneLoadManager == SceneLoadManager.Instance)
        {
            return;
        }

        if (_sceneLoadManager != null)
        {
            _sceneLoadManager.OnSceneLoadComplete -= HandleSceneLoadComplete;
        }

        _sceneLoadManager = SceneLoadManager.Instance;

        if (_sceneLoadManager != null)
        {
            _sceneLoadManager.OnSceneLoadComplete += HandleSceneLoadComplete;
        }
    }

    private void RefreshCurrentBgm()
    {
        if (TryResolveCurrentStageBgm(out BGMKey stageKey))
        {
            PlayIfNeeded(stageKey);
            return;
        }

        ApplySceneBgm(ResolveCurrentSceneType());
    }

    private void ApplySceneBgm(ESceneType sceneType)
    {
        if (sceneType == ESceneType.Gameplay && TryResolveCurrentStageBgm(out BGMKey stageKey))
        {
            PlayIfNeeded(stageKey);
            return;
        }

        BGMKey key = sceneType switch
        {
            ESceneType.MainMenu => BGMKey.Main,
            ESceneType.Lobby => BGMKey.Lobby,
            ESceneType.WaitingRoom => BGMKey.WaitingRoom,
            _ => BGMKey.None
        };

        if (key != BGMKey.None)
        {
            PlayIfNeeded(key);
        }
    }

    private void ApplyStageBgm()
    {
        if (!TryResolveCurrentStageBgm(out BGMKey key))
        {
            return;
        }

        PlayIfNeeded(key);
    }

    private bool TryResolveCurrentStageBgm(out BGMKey key)
    {
        key = BGMKey.None;

        string stageId = string.Empty;
        if (StageFlowManager.Instance?.CurrentStageData != null)
        {
            stageId = StageFlowManager.Instance.CurrentStageData.StageId;
        }
        else if (StageSceneConfig.Instance != null)
        {
            stageId = StageSceneConfig.Instance.StageId;
        }

        if (string.IsNullOrWhiteSpace(stageId))
        {
            return false;
        }

        key = stageId switch
        {
            "1" => BGMKey.Halloween,
            "Stage1" => BGMKey.Halloween,
            "2" => BGMKey.Military,
            "Stage2" => BGMKey.Military,
            "3" => BGMKey.Winter,
            "Stage3" => BGMKey.Winter,
            "4" => BGMKey.Hospital,
            "Stage4" => BGMKey.Hospital,
            _ => BGMKey.None
        };

        return key != BGMKey.None;
    }

    private void PlayIfNeeded(BGMKey key)
    {
        if (key == BGMKey.None || _currentKey == key || SoundManager.Instance == null)
        {
            return;
        }

        _currentKey = key;
        SoundManager.Instance.Play(key);
    }

    private ESceneType ResolveCurrentSceneType()
    {
        if (_sceneLoadManager != null)
        {
            return _sceneLoadManager.CurrentSceneType;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName switch
        {
            "Main" => ESceneType.MainMenu,
            "Lobby" => ESceneType.Lobby,
            "WaitingRoom" => ESceneType.WaitingRoom,
            "GameScene" => ESceneType.Gameplay,
            "Cutscene" => ESceneType.Cutscene,
            _ => ESceneType.Gameplay
        };
    }
}
