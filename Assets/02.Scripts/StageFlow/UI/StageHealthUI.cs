using DG.Tweening;
using DontDillyDally.StageFlow;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class StageHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Slider _healthGauge;
    [SerializeField] private TextMeshProUGUI _healthText;

    [Header("Health Tween")]
    [SerializeField] private float _healthTweenDuration = 0.25f;
    [SerializeField] private Ease _healthTweenEase = Ease.OutCubic;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private StageFlowManager _stageFlowManager;
    private Tween _healthTween;
    private float _cachedHealth;
    private float _maxHealth;
    private bool _hasAppliedGaugeValue;

    private void Start()
    {
        EnsureUiReferences();
        TryBind();
        RefreshUi(snapGaugeValue: true);
    }

    private void Update()
    {
        if (_stageFlowManager == null)
        {
            TryBind();
            RefreshUi(snapGaugeValue: true);
        }
    }

    private void OnDestroy()
    {
        if (_stageFlowManager != null)
        {
            _stageFlowManager.OnStageDataChanged -= HandleStageDataChanged;
        }

        _disposables.Dispose();
        _healthTween?.Kill();
        _healthTween = null;
    }

    private void TryBind()
    {
        if (_stageFlowManager != null || StageFlowManager.Instance == null)
        {
            return;
        }

        _stageFlowManager = StageFlowManager.Instance;
        _stageFlowManager.OnStageDataChanged += HandleStageDataChanged;

        if (_stageFlowManager.CurrentStageData != null)
        {
            HandleStageDataChanged(_stageFlowManager.CurrentStageData);
        }
        else
        {
            _cachedHealth = Mathf.Clamp(_stageFlowManager.PatientHealth.Value, 0f, _maxHealth);
        }

        _stageFlowManager.PatientHealth
            .Subscribe(HandlePatientHealthChanged)
            .AddTo(_disposables);

        _stageFlowManager.CurrentPhase
            .Subscribe(_ => RefreshUi(snapGaugeValue: true))
            .AddTo(_disposables);

        _stageFlowManager.CurrentPatientIndex
            .Subscribe(_ => RefreshUi(snapGaugeValue: true))
            .AddTo(_disposables);
    }

    private void HandleStageDataChanged(StageRuntimeData stageData)
    {
        if (stageData == null)
        {
            return;
        }

        _maxHealth = Mathf.Max(1f, stageData.MaxPatientHealth);
        _cachedHealth = _stageFlowManager != null
            ? Mathf.Clamp(_stageFlowManager.PatientHealth.Value, 0f, _maxHealth)
            : Mathf.Clamp(_cachedHealth, 0f, _maxHealth);

        RefreshUi(snapGaugeValue: true);
    }

    private void HandlePatientHealthChanged(float health)
    {
        _cachedHealth = Mathf.Clamp(health, 0f, _maxHealth);

        if (!ShouldShowPanel())
        {
            RefreshUi(snapGaugeValue: true);
            return;
        }

        RefreshUi(snapGaugeValue: false);

        float currentGaugeValue = _healthGauge != null ? _healthGauge.value : _cachedHealth;
        if (_cachedHealth > currentGaugeValue)
        {
            AnimateGaugeTo(_cachedHealth);
            return;
        }

        _healthTween?.Kill();
        _healthTween = null;
        SetGaugeValueImmediate(_cachedHealth);
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

    private void RefreshUi(bool snapGaugeValue)
    {
        EnsureUiReferences();

        if (_panelRoot == null)
        {
            return;
        }

        bool shouldShow = ShouldShowPanel();
        _panelRoot.SetActive(shouldShow);

        if (!shouldShow)
        {
            _healthTween?.Kill();
            _healthTween = null;

            if (snapGaugeValue)
            {
                SetGaugeValueImmediate(_cachedHealth);
            }

            return;
        }

        if (_healthGauge != null)
        {
            _healthGauge.minValue = 0f;
            _healthGauge.maxValue = _maxHealth;

            if (snapGaugeValue || !_hasAppliedGaugeValue)
            {
                SetGaugeValueImmediate(_cachedHealth);
            }
        }

        if (_healthText != null)
        {
            _healthText.text = "환자 체력";
        }
    }

    private void AnimateGaugeTo(float targetValue)
    {
        if (_healthGauge == null)
        {
            return;
        }

        if (!_hasAppliedGaugeValue)
        {
            SetGaugeValueImmediate(targetValue);
            return;
        }

        _healthTween?.Kill();
        _healthTween = _healthGauge
            .DOValue(targetValue, _healthTweenDuration)
            .SetEase(_healthTweenEase)
            .OnKill(() => _healthTween = null);
    }

    private void SetGaugeValueImmediate(float value)
    {
        if (_healthGauge == null)
        {
            return;
        }

        _healthGauge.value = value;
        _hasAppliedGaugeValue = true;
    }

    private bool ShouldShowPanel()
    {
        return _stageFlowManager != null &&
               (_stageFlowManager.CurrentPhase.Value == EStagePhase.Cutscene ||
                _stageFlowManager.CurrentPhase.Value == EStagePhase.Playing ||
                _stageFlowManager.CurrentPhase.Value == EStagePhase.PatientTransition);
    }
}
