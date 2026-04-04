using DG.Tweening;
using DontDillyDally.StageFlow;
using UniRx;
using UnityEngine;

public enum EntranceSelectionMode
{
    Random,
    Fixed
}

public class PatientEntranceDirector : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private EntranceSelectionMode _selectionMode = EntranceSelectionMode.Random;
    [SerializeField] private PatientEntranceBase _fixedEntrance;

    [Header("Entrance Pool (for Random mode)")]
    [SerializeField] private PatientEntranceBase[] _entrances;

    [Header("Target")]
    [SerializeField] private Transform _bedTransform;

    [Header("Debug")]
    [SerializeField] private bool _debugMode;
    [SerializeField] private float _debugDelay = 1.0f;

    private readonly CompositeDisposable _disposables = new();

    private StageFlowManager _stageFlowManager;
    private PatientEntranceBase _activeEntrance;
    private Vector3 _finalPosition;
    private Quaternion _finalRotation;
    private bool _hasPlayed;
    private bool _isBound;

    private void Awake()
    {
        if (_bedTransform == null)
        {
            Debug.LogError("[PatientEntranceDirector] Bed transform is not assigned.");
            enabled = false;
            return;
        }

        CacheFinalPose();
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

    private void CacheFinalPose()
    {
        _finalPosition = _bedTransform.position;
        _finalRotation = _bedTransform.rotation;
    }

    // Late-bind pattern matching StageWaitNoticeUI.
    private void TryBindToStageFlow()
    {
        if (StageFlowManager.Instance == null)
        {
            return;
        }

        _stageFlowManager = StageFlowManager.Instance;
        _isBound = true;

        EStagePhase currentPhase = _stageFlowManager.CurrentPhase.Value;

        // Already past countdown: snap to final position.
        if (currentPhase >= EStagePhase.Playing)
        {
            _hasPlayed = true;
            SnapBedToFinalPose();
            return;
        }

        // Subscribe to phase changes.
        _stageFlowManager.CurrentPhase
            .Subscribe(OnPhaseChanged)
            .AddTo(_disposables);
    }

    private void OnPhaseChanged(EStagePhase phase)
    {
        if (phase == EStagePhase.Countdown && !_hasPlayed)
        {
            PlaySelectedEntrance();
        }
        else if (phase == EStagePhase.Playing)
        {
            ForceCompleteIfNeeded();
        }
    }

    private void PlaySelectedEntrance()
    {
        _hasPlayed = true;

        PatientEntranceBase entrance = SelectEntrance();
        if (entrance == null)
        {
            Debug.LogWarning("[PatientEntranceDirector] No valid entrance found. Snapping to final position.");
            SnapBedToFinalPose();
            return;
        }

        _activeEntrance = entrance;
        _activeEntrance.Play(_bedTransform, _finalPosition, _finalRotation);
    }

    private PatientEntranceBase SelectEntrance()
    {
        if (_selectionMode == EntranceSelectionMode.Fixed)
        {
            return _fixedEntrance;
        }

        if (_entrances == null || _entrances.Length == 0)
        {
            return _fixedEntrance;
        }

        int index = Random.Range(0, _entrances.Length);
        return _entrances[index];
    }

    private void ForceCompleteIfNeeded()
    {
        if (_activeEntrance == null)
        {
            return;
        }

        _activeEntrance.ForceComplete(_bedTransform, _finalPosition, _finalRotation);
        _activeEntrance = null;
    }

    private void SnapBedToFinalPose()
    {
        if (_bedTransform == null)
        {
            return;
        }

        _bedTransform.position = _finalPosition;
        _bedTransform.rotation = _finalRotation;
    }

    // ── Debug Mode ───────────────────────────────────────────────

    private void StartDebugMode()
    {
        if (_entrances == null || _entrances.Length == 0)
        {
            Debug.LogWarning("[PatientEntranceDirector] No entrances assigned for debug mode.");
            return;
        }

        DOVirtual.DelayedCall(_debugDelay, () => PlayEntranceByIndex(0));
    }

    private void HandleDebugInput()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayEntranceByIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayEntranceByIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayEntranceByIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayEntranceByIndex(3);
        }
#endif
    }

    private void PlayEntranceByIndex(int index)
    {
        if (_entrances == null || index < 0 || index >= _entrances.Length)
        {
            Debug.LogWarning($"[PatientEntranceDirector] Invalid entrance index: {index}");
            return;
        }

        ForceCompleteIfNeeded();

        _activeEntrance = _entrances[index];
        _activeEntrance.Play(_bedTransform, _finalPosition, _finalRotation);

        Debug.Log($"[PatientEntranceDirector] Playing entrance: {_activeEntrance.GetType().Name}");
    }
}
