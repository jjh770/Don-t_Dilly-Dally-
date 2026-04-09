using DontDillyDally.StageFlow;
using TMPro;
using UniRx;
using UnityEngine;

public class StageWaitNoticeUI : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private string _loadingMessage = "데이터 준비 중입니다...";

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private StageFlowManager _stageFlowManager;

    private void Start()
    {
        EnsureReferences();
        if (!TryBind())
        {
            StageFlowBootstrapper.StageFlowReady += HandleStageFlowReady;
        }

        RefreshUi();
    }

    private void Update()
    {
        RefreshUi();
    }

    private void OnDestroy()
    {
        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;
        _disposables.Dispose();
    }

    private bool TryBind()
    {
        if (_stageFlowManager != null ||
            StageFlowBootstrapper.Instance == null ||
            !StageFlowBootstrapper.Instance.IsStageFlowReady ||
            StageFlowManager.Instance == null ||
            !StageFlowManager.Instance.IsInitialized)
        {
            return false;
        }

        _stageFlowManager = StageFlowManager.Instance;

        _stageFlowManager.CurrentPhase
            .Subscribe(_ =>
            {
                RefreshUi();
            })
            .AddTo(_disposables);

        _stageFlowManager.CountdownStartTime
            .Subscribe(_ =>
            {
                RefreshUi();
            })
            .AddTo(_disposables);

        _stageFlowManager.CountdownDuration
            .Subscribe(_ =>
            {
                RefreshUi();
            })
            .AddTo(_disposables);

        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;
        return true;
    }

    private void HandleStageFlowReady()
    {
        if (TryBind())
        {
            RefreshUi();
        }
    }

    private void EnsureReferences()
    {
        if (_panelRoot == null)
        {
            Transform firstChild = transform.childCount > 0 ? transform.GetChild(0) : null;
            _panelRoot = firstChild != null ? firstChild.gameObject : gameObject;
        }

        if (_countdownText == null)
        {
            _countdownText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void RefreshUi()
    {
        EnsureReferences();

        if (_panelRoot == null)
        {
            return;
        }

        bool isLoading = _stageFlowManager == null ||
                         _stageFlowManager.CurrentPhase.Value == EStagePhase.Loading;
        bool isCountdown = _stageFlowManager != null &&
                           _stageFlowManager.CurrentPhase.Value == EStagePhase.Countdown;
        bool shouldShow = isLoading || isCountdown;

        _panelRoot.SetActive(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        if (_countdownText == null)
        {
            return;
        }

        if (isLoading)
        {
            _countdownText.text = _loadingMessage;
            return;
        }

        float remainingTime = _stageFlowManager.GetRemainingCountdownTime();
        int displayCount = Mathf.CeilToInt(remainingTime);

        if (displayCount > 0)
        {
            _countdownText.text = displayCount.ToString();
            return;
        }

        _countdownText.text = string.Empty;
    }
}
