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
    private float _lastSyncedTime;
    private float _lastSyncRealtime;
    private float _totalTimeLimit;
    private EStagePhase _currentPhase = EStagePhase.None;

    private void Start()
    {
        TryBind();
        RefreshTimerText();
    }

    private void Update()
    {
        if (_stageFlowManager == null)
        {
            TryBind();
            return;
        }

        RefreshTimerText();
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// StageFlowManager가 준비된 뒤 구독을 연결합니다.
    /// 클라이언트는 마지막 동기화 시각 기준으로 로컬 표시만 보간합니다.
    /// </summary>
    private void TryBind()
    {
        if (_stageFlowManager != null || StageFlowManager.Instance == null || !StageFlowManager.Instance.IsInitialized)
        {
            return;
        }

        _stageFlowManager = StageFlowManager.Instance;
        _lastSyncedTime = _stageFlowManager.StageTimer.Value;
        _lastSyncRealtime = Time.unscaledTime;
        _currentPhase = _stageFlowManager.CurrentPhase.Value;
        _totalTimeLimit = _stageFlowManager.CurrentStageData?.Settings?.TotalTimeLimitSec ?? _lastSyncedTime;

        _stageFlowManager.StageTimer
            .Subscribe(time =>
            {
                _lastSyncedTime = Mathf.Max(0f, time);
                _lastSyncRealtime = Time.unscaledTime;
                RefreshTimerText();
            })
            .AddTo(_disposables);

        _stageFlowManager.CurrentPhase
            .Subscribe(phase =>
            {
                if (_currentPhase == EStagePhase.Playing && phase != EStagePhase.Playing)
                {
                    _lastSyncedTime = GetDisplayTime();
                    _lastSyncRealtime = Time.unscaledTime;
                }

                _currentPhase = phase;
                RefreshTimerText();
            })
            .AddTo(_disposables);
    }

    private void RefreshTimerText()
    {
        bool shouldShow = _stageFlowManager != null &&
                          (_currentPhase == EStagePhase.Playing ||
                           _currentPhase == EStagePhase.PatientTransition ||
                           _currentPhase == EStagePhase.StageClear);

        if (_timerText != null)
        {
            _timerText.gameObject.SetActive(shouldShow);
        }

        if (_timerSlider != null)
        {
            _timerSlider.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        float displayTime = GetDisplayTime();

        if (_timerText != null)
        {
            int totalSeconds = Mathf.CeilToInt(displayTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        if (_timerSlider != null && _totalTimeLimit > 0f)
        {
            _timerSlider.value = displayTime / _totalTimeLimit;
        }
    }

    private float GetDisplayTime()
    {
        if (_stageFlowManager == null)
        {
            return 0f;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            return Mathf.Max(0f, _stageFlowManager.LocalRemainingTime);
        }

        if (_currentPhase == EStagePhase.Playing)
        {
            float elapsedTime = Time.unscaledTime - _lastSyncRealtime;
            return Mathf.Max(0f, _lastSyncedTime - elapsedTime);
        }

        return Mathf.Max(0f, _lastSyncedTime);
    }
}
