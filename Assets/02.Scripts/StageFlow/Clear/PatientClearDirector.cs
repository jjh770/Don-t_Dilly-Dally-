using DG.Tweening;
using DontDillyDally.StageFlow;
using UniRx;
using UnityEngine;

public enum ClearSelectionMode
{
    Random,
    Fixed
}

public class PatientClearDirector : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private ClearSelectionMode _selectionMode = ClearSelectionMode.Random;
    [SerializeField] private PatientClearBase _fixedClear;

    [Header("Clear Pool (for Random mode)")]
    [SerializeField] private PatientClearBase[] _clears;

    [Header("Target")]
    [SerializeField] private Transform _patientRoot;
    [SerializeField] private Transform _bedTransform;
    [SerializeField] private Transform _patientTransform;

    [Header("Debug")]
    [SerializeField] private bool _debugMode;

    private readonly CompositeDisposable _disposables = new();

    private StageFlowManager _stageFlowManager;
    private PatientClearBase _activeClear;
    private bool _hasPlayed;
    private bool _isBound;

    // Clear 시퀀스가 종료(자연 완료 또는 강제 Kill)되면 호출. EntranceDirector가 구독.
    public event System.Action OnClearFinished;

    // Clear 연출이 현재 재생 중인지 여부.
    public bool IsClearPlaying => _activeClear != null;

    // 디버그 원복용 초기 상태 캐싱.
    private Vector3 _initialRootPosition;
    private Quaternion _initialRootRotation;
    private Vector3 _initialBedLocalPosition;
    private Quaternion _initialBedLocalRotation;
    private Vector3 _initialPatientLocalPosition;
    private Vector3 _initialPatientLocalScale;
    private Transform _initialPatientParent;

    private void Awake()
    {
        if (_patientRoot == null || _bedTransform == null || _patientTransform == null)
        {
            Debug.LogError("[PatientClearDirector] Transform references are not assigned.");
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
        if (StageFlowManager.Instance == null)
        {
            return;
        }

        _stageFlowManager = StageFlowManager.Instance;
        _isBound = true;

        _stageFlowManager.CurrentPhase
            .Subscribe(OnPhaseChanged)
            .AddTo(_disposables);
    }

    private void OnPhaseChanged(EStagePhase phase)
    {
        if (!_hasPlayed &&
            (phase == EStagePhase.PatientTransition || phase == EStagePhase.StageClear))
        {
            PlaySelectedClear();
        }
        // Playing 페이즈에서 Clear를 강제 중단하지 않는다.
        // Clear 시퀀스의 OnKill 콜백이 자연 완료/강제 Kill 양쪽에서 상태 정리 + OnClearFinished 발화.
    }

    private void PlaySelectedClear()
    {
        _hasPlayed = true;

        PatientClearBase clear = SelectClear();
        if (clear == null)
        {
            Debug.LogWarning("[PatientClearDirector] No valid clear animation found.");
            HandleClearSequenceEnded();
            return;
        }

        _activeClear = clear;
        Sequence seq = _activeClear.Play(_patientRoot, _bedTransform, _patientTransform);
        if (seq != null)
        {
            // OnKill은 자연 완료(auto-kill) + 외부 Kill() 양쪽에서 정확히 한 번 호출됨.
            // 기존 구현체들의 OnComplete(VFX 정리)와 슬롯이 달라 충돌 없음.
            seq.OnKill(HandleClearSequenceEnded);
        }
        else
        {
            // Play가 Sequence를 반환하지 않은 예외 케이스 — 즉시 상태 정리.
            HandleClearSequenceEnded();
        }
    }

    private void HandleClearSequenceEnded()
    {
        _activeClear = null;
        _hasPlayed = false;
        OnClearFinished?.Invoke();
    }

    private PatientClearBase SelectClear()
    {
        if (_selectionMode == ClearSelectionMode.Fixed)
        {
            return _fixedClear;
        }

        if (_clears == null || _clears.Length == 0)
        {
            return _fixedClear;
        }

        // 마스터가 생성한 시드로 동기화 (모든 클라이언트 동일 결과).
        // Entrance/Death와 다른 결과를 위해 rng.Next() 2회 스킵.
        int seed = _stageFlowManager != null ? _stageFlowManager.DirectionSeed : 0;
        var rng = new System.Random(seed);
        rng.Next();
        rng.Next();
        int index = rng.Next(0, _clears.Length);
        return _clears[index];
    }

    private void ForceCompleteIfNeeded()
    {
        if (_activeClear == null)
        {
            return;
        }

        _activeClear.ForceComplete(_patientRoot, _bedTransform, _patientTransform);
        // _activeClear 및 _hasPlayed 정리와 OnClearFinished 발화는
        // 시퀀스 Kill로 인해 호출되는 HandleClearSequenceEnded에서 수행됨.
    }

    // ── Debug Mode ───────────────────────────────────────────────

    private void StartDebugMode()
    {
        if (_clears == null || _clears.Length == 0)
        {
            Debug.LogWarning("[PatientClearDirector] No clears assigned for debug mode.");
        }
    }

    private void HandleDebugInput()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetToInitialState();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            PlayClearByIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            PlayClearByIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            PlayClearByIndex(2);
        }
#endif
    }

    private void CacheInitialState()
    {
        _initialRootPosition = _patientRoot.position;
        _initialRootRotation = _patientRoot.rotation;
        _initialBedLocalPosition = _bedTransform.localPosition;
        _initialBedLocalRotation = _bedTransform.localRotation;
        _initialPatientLocalPosition = _patientTransform.localPosition;
        _initialPatientLocalScale = _patientTransform.localScale;
        _initialPatientParent = _patientTransform.parent;
    }

    private void ResetToInitialState()
    {
        ForceCompleteIfNeeded();

        if (_patientTransform.parent != _initialPatientParent)
        {
            _patientTransform.SetParent(_initialPatientParent, worldPositionStays: false);
        }

        _patientRoot.position = _initialRootPosition;
        _patientRoot.rotation = _initialRootRotation;
        _bedTransform.localPosition = _initialBedLocalPosition;
        _bedTransform.localRotation = _initialBedLocalRotation;
        _patientTransform.localPosition = _initialPatientLocalPosition;
        _patientTransform.localScale = _initialPatientLocalScale;

        _bedTransform.gameObject.SetActive(true);
        _patientTransform.gameObject.SetActive(true);

        _hasPlayed = false;

        Debug.Log("[PatientClearDirector] Reset to initial state.");
    }

    private void PlayClearByIndex(int index)
    {
        if (_clears == null || index < 0 || index >= _clears.Length)
        {
            Debug.LogWarning($"[PatientClearDirector] Invalid clear index: {index}");
            return;
        }

        ForceCompleteIfNeeded();
        ResetToInitialState();

        _hasPlayed = true;
        _activeClear = _clears[index];
        _activeClear.Play(_patientRoot, _bedTransform, _patientTransform);

        Debug.Log($"[PatientClearDirector] Playing clear: {_activeClear.GetType().Name}");
    }
}
