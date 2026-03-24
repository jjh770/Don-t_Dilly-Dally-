using DontDillyDally.StageFlow;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class StageHealthUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Slider _healthGauge;
    [SerializeField] private TextMeshProUGUI _healthText;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private StageFlowManager _stageFlowManager;
    private float _cachedHealth;
    private float _maxHealth = 100f;

    private void Start()
    {
        EnsureUiReferences();
        TryBind();
        RefreshUi();
    }

    private void Update()
    {
        if (_stageFlowManager == null)
        {
            TryBind();
            RefreshUi();
        }
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// StageFlowManager의 체력/페이즈 변경을 구독해서 현재 환자 체력 UI를 갱신합니다.
    /// </summary>
    private void TryBind()
    {
        if (_stageFlowManager != null || StageFlowManager.Instance == null)
        {
            return;
        }

        _stageFlowManager = StageFlowManager.Instance;

        if (_stageFlowManager.CurrentStageData != null)
        {
            _maxHealth = Mathf.Max(1f, _stageFlowManager.CurrentStageData.InitialPatientHealth);
        }

        _cachedHealth = Mathf.Clamp(_stageFlowManager.PatientHealth.Value, 0f, _maxHealth);

        _stageFlowManager.PatientHealth
            .Subscribe(health =>
            {
                _cachedHealth = Mathf.Clamp(health, 0f, _maxHealth);
                RefreshUi();
            })
            .AddTo(_disposables);

        _stageFlowManager.CurrentPhase
            .Subscribe(_ => RefreshUi())
            .AddTo(_disposables);

        _stageFlowManager.CurrentPatientIndex
            .Subscribe(_ =>
            {
                if (_stageFlowManager.CurrentStageData != null)
                {
                    _maxHealth = Mathf.Max(1f, _stageFlowManager.CurrentStageData.InitialPatientHealth);
                }

                _cachedHealth = Mathf.Clamp(_stageFlowManager.PatientHealth.Value, 0f, _maxHealth);
                RefreshUi();
            })
            .AddTo(_disposables);

        RefreshUi();
    }

    private void EnsureUiReferences()
    {
        if (_panelRoot == null)
        {
            _panelRoot = gameObject;
        }

        if (_healthGauge == null)
        {
            _healthGauge = GetComponentInChildren<Slider>(true);
        }

        if (_healthText == null)
        {
            _healthText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void RefreshUi()
    {
        EnsureUiReferences();

        if (_panelRoot == null)
        {
            return;
        }

        bool shouldShow = _stageFlowManager != null &&
                          (_stageFlowManager.CurrentPhase.Value == EStagePhase.Cutscene ||
                           _stageFlowManager.CurrentPhase.Value == EStagePhase.Playing ||
                           _stageFlowManager.CurrentPhase.Value == EStagePhase.PatientTransition);

        _panelRoot.SetActive(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        if (_healthGauge != null)
        {
            _healthGauge.minValue = 0f;
            _healthGauge.maxValue = _maxHealth;
            _healthGauge.value = _cachedHealth;
        }

        if (_healthText != null)
        {
            _healthText.text = $"체력";
        }
    }
}
