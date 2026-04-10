using DG.Tweening;
using DontDillyDally.StageFlow;
using Photon.Pun;
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

        // Playing 복귀 시 트윈만 Kill하고 플래그 리셋.
        // 위치 원복은 EntranceDirector가 담당하므로 여기서 건드리지 않는다.
        if (phase == EStagePhase.Playing && _hasPlayed)
        {
            KillActiveTweensOnly();
            _hasPlayed = false;
        }
    }

    private void PlaySelectedClear()
    {
        _hasPlayed = true;

        PatientClearBase clear = SelectClear();
        if (clear == null)
        {
            Debug.LogWarning("[PatientClearDirector] No valid clear animation found.");
            return;
        }

        _activeClear = clear;
        _activeClear.Play(_patientRoot, _bedTransform, _patientTransform);
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

        // 멀티플레이어 동기화를 위해 PhotonNetwork.Time 기반 시드 사용.
        // 인접 시드의 첫 출력이 비슷한 .NET Random 특성 때문에 첫 두 호출은 버린다.
        int seed = (int)(PhotonNetwork.Time * 1000);
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
        _activeClear = null;
    }

    // 위치 원복 없이 트윈만 Kill.
    // Playing 복귀 시 EntranceDirector와의 순서 경합 방지용.
    private void KillActiveTweensOnly()
    {
        if (_activeClear == null)
        {
            return;
        }

        _patientRoot.DOKill();
        _bedTransform.DOKill();
        _patientTransform.DOKill();
        _activeClear = null;
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
