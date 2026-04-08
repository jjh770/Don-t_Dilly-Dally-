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

    private readonly CompositeDisposable _disposables = new();

    private StageFlowManager _stageFlowManager;
    private PatientDeathBase _activeDeath;
    private bool _hasPlayed;
    private bool _isBound;
    private bool _isPatientDeath;

    // 디버그 원복용 초기 상태 캐싱.
    private Vector3 _initialRootPosition;
    private Quaternion _initialRootRotation;
    private Vector3 _initialBedLocalPosition;
    private Vector3 _initialPatientLocalPosition;
    private Vector3 _initialPatientLocalScale;
    private Transform _initialPatientParent;

    private void Awake()
    {
        if (_patientRoot == null || _bedTransform == null || _patientTransform == null)
        {
            Debug.LogError("[PatientDeathDirector] Transform references are not assigned.");
            enabled = false;
            return;
        }

        CacheInitialState();
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
        if (StageFlowManager.Instance == null || EventManager.Instance == null)
        {
            return;
        }

        _stageFlowManager = StageFlowManager.Instance;
        _isBound = true;

        EventManager.Instance.OnEventPublished += OnGameEventPublished;
        _disposables.Add(Disposable.Create(() =>
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventPublished -= OnGameEventPublished;
            }
        }));

        // 늦은 바인딩 시 이미 PatientDeath 이벤트가 발행되었는지 로그에서 확인.
        foreach (GameEvent loggedEvent in EventManager.Instance.EventLog)
        {
            if (loggedEvent.Type == EventType.PatientDeath)
            {
                _isPatientDeath = true;
                break;
            }
        }

        _stageFlowManager.CurrentPhase
            .Subscribe(OnPhaseChanged)
            .AddTo(_disposables);
    }

    private void OnGameEventPublished(GameEvent gameEvent)
    {
        if (gameEvent.Type == EventType.PatientDeath)
        {
            _isPatientDeath = true;
            TryPlayDeath();
        }
    }

    private void OnPhaseChanged(EStagePhase phase)
    {
        TryPlayDeath();
    }

    private void TryPlayDeath()
    {
        if (_hasPlayed || !_isPatientDeath)
        {
            return;
        }

        if (_stageFlowManager == null || _stageFlowManager.CurrentPhase.Value != EStagePhase.GameOver)
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
        // 인접 시드의 첫 출력이 비슷한 .NET Random 특성 때문에 첫 호출은 버린다.
        int seed = (int)(PhotonNetwork.Time * 1000);
        var rng = new System.Random(seed);
        rng.Next();
        int index = rng.Next(0, _deaths.Length);
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
        }
    }

    private void HandleDebugInput()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetToInitialState();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
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

    private void CacheInitialState()
    {
        _initialRootPosition = _patientRoot.position;
        _initialRootRotation = _patientRoot.rotation;
        _initialBedLocalPosition = _bedTransform.localPosition;
        _initialPatientLocalPosition = _patientTransform.localPosition;
        _initialPatientLocalScale = _patientTransform.localScale;
        _initialPatientParent = _patientTransform.parent;
    }

    private void ResetToInitialState()
    {
        ForceCompleteIfNeeded();

        // 환자를 원래 부모로 복원 (천사 승천 등에서 분리된 경우).
        if (_patientTransform.parent != _initialPatientParent)
        {
            _patientTransform.SetParent(_initialPatientParent, worldPositionStays: false);
        }

        _patientRoot.position = _initialRootPosition;
        _patientRoot.rotation = _initialRootRotation;
        _bedTransform.localPosition = _initialBedLocalPosition;
        _patientTransform.localPosition = _initialPatientLocalPosition;
        _patientTransform.localScale = _initialPatientLocalScale;

        // 모든 오브젝트 다시 활성화.
        _bedTransform.gameObject.SetActive(true);
        _patientTransform.gameObject.SetActive(true);
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
    }
}
