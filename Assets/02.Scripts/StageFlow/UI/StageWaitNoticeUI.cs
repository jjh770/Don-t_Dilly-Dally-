using DontDillyDally.StageFlow;
using TMPro;
using UniRx;
using UnityEngine;

public class StageWaitNoticeUI : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TextMeshProUGUI _countdownText;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private StageFlowManager _stageFlowManager;

    private void Start()
    {
        EnsureReferences();
        TryBind();
        RefreshUi();
    }

    private void Update()
    {
        if (_stageFlowManager == null)
        {
            TryBind();
        }

        RefreshUi();
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    private void TryBind()
    {
        if (_stageFlowManager != null || StageFlowManager.Instance == null)
        {
            return;
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

        bool shouldShow = _stageFlowManager != null &&
                          _stageFlowManager.CurrentPhase.Value == EStagePhase.Countdown;

        _panelRoot.SetActive(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        if (_countdownText == null)
        {
            return;
        }

        float remainingTime = _stageFlowManager.GetRemainingCountdownTime();
        int displayCount = Mathf.CeilToInt(remainingTime);

        if (displayCount > 0)
        {
            _countdownText.text = displayCount.ToString();
            return;
        }
    }
}
