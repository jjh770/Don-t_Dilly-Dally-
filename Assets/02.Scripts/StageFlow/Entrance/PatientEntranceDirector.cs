using DG.Tweening;
using DontDillyDally.StageFlow;
using Photon.Pun;
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

        // Hide bed until the entrance animation begins.
        SetBedActive(false);
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

        // Already past countdown: snap to final position and show immediately.
        if (currentPhase >= EStagePhase.Playing)
        {
            _hasPlayed = true;
            SetBedActive(true);
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
            SetBedActive(true);
            SnapBedToFinalPose();
            return;
        }

        SetBedActive(true);
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

        // 멀티플레이어 동기화를 위해 PhotonNetwork.Time 기반 시드 사용.
        int seed = (int)(PhotonNetwork.Time * 1000);
        int index = new System.Random(seed).Next(0, _entrances.Length);
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

    private void SetBedActive(bool active)
    {
        if (_bedTransform == null)
        {
            return;
        }

        if (_bedTransform.gameObject.activeSelf != active)
        {
            _bedTransform.gameObject.SetActive(active);
        }
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

        SetBedActive(true);
        _activeEntrance = _entrances[index];
        _activeEntrance.Play(_bedTransform, _finalPosition, _finalRotation);

        Debug.Log($"[PatientEntranceDirector] Playing entrance: {_activeEntrance.GetType().Name}");
    }
}
