using DG.Tweening;
using DontDillyDally.StageFlow;
using Photon.Pun;
using UniRx;
using UnityEngine;

public enum DeathSelectionMode
{
    Random,
    Fixed
}

public class PatientDeathDirector : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private DeathSelectionMode _selectionMode = DeathSelectionMode.Random;
    [SerializeField] private PatientDeathBase _fixedDeath;

    [Header("Death Pool (for Random mode)")]
    [SerializeField] private PatientDeathBase[] _deaths;

    [Header("Target")]
    [SerializeField] private Transform _patientRoot;
    [SerializeField] private Transform _bedTransform;
    [SerializeField] private Transform _patientTransform;

    [Header("Debug")]
    [SerializeField] private bool _debugMode;
    [SerializeField] private float _debugDelay = 1.0f;

    private readonly CompositeDisposable _disposables = new();

    private StageFlowManager _stageFlowManager;
    private PatientDeathBase _activeDeath;
    private bool _hasPlayed;
    private bool _isBound;
    private bool _isPatientDeath;

    private void Awake()
    {
        if (_patientRoot == null || _bedTransform == null || _patientTransform == null)
        {
            Debug.LogError("[PatientDeathDirector] Transform references are not assigned.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (_debugMode)
        {
            StartDebugMode();
        }
    }

    private void Update()
    {
        if (_debugMode)
        {
            HandleDebugInput();
            return;
        }

        if (!_isBound)
        {
            TryBindToStageFlow();
        }
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
        ForceCompleteIfNeeded();
    }

    private void TryBindToStageFlow()
    {
        if (StageFlowManager.Instance == null)
        {
            return;
        }

        _stageFlowManager = StageFlowManager.Instance;
        _isBound = true;

        SubscribeToPatientDeathEvent();

        _stageFlowManager.CurrentPhase
            .Subscribe(OnPhaseChanged)
            .AddTo(_disposables);
    }

    private void SubscribeToPatientDeathEvent()
    {
        if (EventManager.Instance == null)
        {
            return;
        }

        EventManager.Instance.OnEventPublished += OnGameEventPublished;
        _disposables.Add(Disposable.Create(() =>
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventPublished -= OnGameEventPublished;
            }
        }));
    }

    private void OnGameEventPublished(GameEvent gameEvent)
    {
        if (gameEvent.Type == EventType.PatientDeath)
        {
            _isPatientDeath = true;
        }
    }

    private void OnPhaseChanged(EStagePhase phase)
    {
        if (phase != EStagePhase.GameOver || _hasPlayed || !_isPatientDeath)
        {
            return;
        }

        PlaySelectedDeath();
    }

    private void PlaySelectedDeath()
    {
        _hasPlayed = true;

        PatientDeathBase death = SelectDeath();
        if (death == null)
        {
            Debug.LogWarning("[PatientDeathDirector] No valid death animation found.");
            return;
        }

        _activeDeath = death;
        _activeDeath.Play(_patientRoot, _bedTransform, _patientTransform);
    }

    private PatientDeathBase SelectDeath()
    {
        if (_selectionMode == DeathSelectionMode.Fixed)
        {
            return _fixedDeath;
        }

        if (_deaths == null || _deaths.Length == 0)
        {
            return _fixedDeath;
        }

        // 멀티플레이어 동기화를 위해 PhotonNetwork.Time 기반 시드 사용.
        int seed = (int)(PhotonNetwork.Time * 1000);
        int index = new System.Random(seed).Next(0, _deaths.Length);
        return _deaths[index];
    }

    private void ForceCompleteIfNeeded()
    {
        if (_activeDeath == null)
        {
            return;
        }

        _activeDeath.ForceComplete(_patientRoot, _bedTransform, _patientTransform);
        _activeDeath = null;
    }

    // ── Debug Mode ───────────────────────────────────────────────

    private void StartDebugMode()
    {
        if (_deaths == null || _deaths.Length == 0)
        {
            Debug.LogWarning("[PatientDeathDirector] No deaths assigned for debug mode.");
            return;
        }

        DOVirtual.DelayedCall(_debugDelay, () => PlayDeathByIndex(0));
    }

    private void HandleDebugInput()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayDeathByIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            PlayDeathByIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            PlayDeathByIndex(2);
        }
#endif
    }

    private void PlayDeathByIndex(int index)
    {
        if (_deaths == null || index < 0 || index >= _deaths.Length)
        {
            Debug.LogWarning($"[PatientDeathDirector] Invalid death index: {index}");
            return;
        }

        ForceCompleteIfNeeded();

        _activeDeath = _deaths[index];
        _activeDeath.Play(_patientRoot, _bedTransform, _patientTransform);

        Debug.Log($"[PatientDeathDirector] Playing death: {_activeDeath.GetType().Name}");
    }
}
