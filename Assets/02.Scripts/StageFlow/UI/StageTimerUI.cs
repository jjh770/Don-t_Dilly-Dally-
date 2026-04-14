using DontDillyDally.StageFlow;
using Photon.Pun;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class StageTimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Slider _timerSlider;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private StageFlowManager _stageFlowManager;
    private EStagePhase _currentPhase = EStagePhase.None;
    private float _totalTimeLimit;
    private float _lastSyncedTime;
    private float _lastSyncRealtime;

    private bool IsBound => _stageFlowManager != null;

    private bool ShouldShowTimer =>
        IsBound &&
        (_currentPhase == EStagePhase.Playing ||
         _currentPhase == EStagePhase.PatientTransition ||
         _currentPhase == EStagePhase.StageClear);

    private void Start()
    {
        if (TryBind() == false)
        {
            StageFlowBootstrapper.StageFlowReady += OnStageFlowReady;
        }
    }

    private void Update()
    {
        if (IsBound == true)
        {
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        StageFlowBootstrapper.StageFlowReady -= OnStageFlowReady;

        if (_stageFlowManager != null)
        {
            _stageFlowManager.OnStageDataChanged -= OnStageDataChanged;
        }

        _disposables.Dispose();
    }

    private bool TryBind()
    {
        if (IsBound || !IsStageFlowReady())
        {
            return false;
        }

        _stageFlowManager = StageFlowManager.Instance;

        InitializeState();
        SubscribeToEvents();

        StageFlowBootstrapper.StageFlowReady -= OnStageFlowReady;
        return true;
    }

    private bool IsStageFlowReady()
    {
        return StageFlowBootstrapper.Instance != null &&
               StageFlowBootstrapper.Instance.IsStageFlowReady &&
               StageFlowManager.Instance != null &&
               StageFlowManager.Instance.IsInitialized;
    }

    private void InitializeState()
    {
        _lastSyncedTime = _stageFlowManager.StageTimer.Value;
        _lastSyncRealtime = Time.unscaledTime;
        _currentPhase = _stageFlowManager.CurrentPhase.Value;

        UpdateTotalTimeLimit();
    }

    private void SubscribeToEvents()
    {
        _stageFlowManager.OnStageDataChanged += OnStageDataChanged;

        _stageFlowManager.StageTimer
            .Subscribe(OnTimerSync)
            .AddTo(_disposables);

        _stageFlowManager.CurrentPhase
            .Subscribe(OnPhaseChanged)
            .AddTo(_disposables);
    }


    private void OnStageFlowReady()
    {
        TryBind();
    }

    private void OnStageDataChanged(StageRuntimeData data)
    {
        UpdateTotalTimeLimit();
    }

    private void OnTimerSync(float time)
    {
        _lastSyncedTime = Mathf.Max(0f, time);
        _lastSyncRealtime = Time.unscaledTime;
        UpdateUI();
    }

    private void OnPhaseChanged(EStagePhase phase)
    {
        if (_currentPhase == EStagePhase.Playing && phase != EStagePhase.Playing)
        {
            _lastSyncedTime = GetDisplayTime();
            _lastSyncRealtime = Time.unscaledTime;
        }

        _currentPhase = phase;
        UpdateUI();
    }

    private void UpdateUI()
    {
        SetVisibility(ShouldShowTimer);

        if (!ShouldShowTimer)
        {
            return;
        }

        float displayTime = GetDisplayTime();
        UpdateTimerText(displayTime);
        UpdateSlider(displayTime);
    }

    private void SetVisibility(bool visible)
    {
        if (_timerText != null)
        {
            _timerText.gameObject.SetActive(visible);
        }

        if (_timerSlider != null)
        {
            _timerSlider.gameObject.SetActive(visible);
        }
    }

    private void UpdateTimerText(float displayTime)
    {
        if (_timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(displayTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        _timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateSlider(float displayTime)
    {
        if (_timerSlider == null || _totalTimeLimit <= 0f)
        {
            return;
        }

        _timerSlider.value = displayTime / _totalTimeLimit;
    }

    private void UpdateTotalTimeLimit()
    {
        float? timeLimit = _stageFlowManager?.CurrentStageData?.Settings?.TotalTimeLimitSec;

        if (timeLimit.HasValue)
        {
            _totalTimeLimit = timeLimit.Value;
        }
    }

    private float GetDisplayTime()
    {
        if (!IsBound)
        {
            return 0f;
        }

        // 마스터 클라이언트
        // 실제 로컬 권한 타이머 사용
        if (PhotonNetwork.IsMasterClient)
        {
            return Mathf.Max(0f, _stageFlowManager.LocalRemainingTime);
        }

        // 일반 클라이언트는
        // 마지막 동기화 시간에서 경과 시간을 빼서 시간이 흐르는 것처럼 표시
        if (_currentPhase == EStagePhase.Playing)
        {
            float elapsed = Time.unscaledTime - _lastSyncRealtime;
            return Mathf.Max(0f, _lastSyncedTime - elapsed);
        }

        return Mathf.Max(0f, _lastSyncedTime);
    }
}
