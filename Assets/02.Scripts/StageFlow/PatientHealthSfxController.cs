using UniRx;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public class PatientHealthSfxController : MonoBehaviour
    {
        private const float CRITICAL_HEALTH_THRESHOLD = 0.3f;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private StageFlowManager _stageFlowManager;
        private bool _isStageFlowBound;
        private float _maxHealth = 1f;

        private AudioSource _criticalSfxHandle;
        private bool _isCriticalActive;
        private AudioSource _deathSfxHandle;
        private bool _isDeathActive;
        private float _previousHealth;
        private bool _hasPreviousHealth;
        private bool _isEmergencyActive;

        private void Start()
        {
            if (!TryBind())
            {
                StageFlowBootstrapper.StageFlowReady += HandleStageFlowReady;
            }
        }

        private void OnDestroy()
        {
            StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;

            if (_stageFlowManager != null && _isStageFlowBound)
            {
                _stageFlowManager.OnStageDataChanged -= HandleStageDataChanged;
                _stageFlowManager.OnStageRewardGranted -= HandleStageRewardGranted;
                _stageFlowManager.OnEmergencyStarted -= HandleEmergencyStarted;
                _stageFlowManager.OnEmergencyEnded -= HandleEmergencyEnded;
            }

            _disposables.Dispose();
            StopCriticalSfx();
            StopDeathSfx();
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
            _stageFlowManager.OnStageDataChanged += HandleStageDataChanged;
            _stageFlowManager.OnStageRewardGranted += HandleStageRewardGranted;
            _stageFlowManager.OnEmergencyStarted += HandleEmergencyStarted;
            _stageFlowManager.OnEmergencyEnded += HandleEmergencyEnded;
            _isStageFlowBound = true;

            if (_stageFlowManager.CurrentStageData != null)
            {
                HandleStageDataChanged(_stageFlowManager.CurrentStageData);
            }

            _stageFlowManager.PatientHealth
                .Subscribe(UpdateCriticalSfx)
                .AddTo(_disposables);

            StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;
            return true;
        }

        private void HandleStageFlowReady()
        {
            TryBind();
        }

        private void HandleStageDataChanged(StageRuntimeData stageData)
        {
            if (stageData == null)
            {
                return;
            }

            _maxHealth = Mathf.Max(1f, stageData.Settings.PatientSettings.InitialPatientHealth);
        }

        private void HandleStageRewardGranted(StageReward reward, StageResult result)
        {
            StopDeathSfx();
        }

        private void HandleEmergencyStarted()
        {
            _isEmergencyActive = true;
            UpdateCriticalSfx(_previousHealth);
        }

        private void HandleEmergencyEnded(bool isSuccess)
        {
            _isEmergencyActive = false;
            UpdateCriticalSfx(_previousHealth);
        }

        private void UpdateCriticalSfx(float health)
        {
            if (_maxHealth <= 0f)
            {
                return;
            }

            float prev = _previousHealth;
            bool hadPrev = _hasPreviousHealth;
            _previousHealth = health;
            _hasPreviousHealth = true;

            float ratio = health / _maxHealth;
            bool shouldPlayCritical = health > 0f && (_isEmergencyActive || ratio < CRITICAL_HEALTH_THRESHOLD);

            if (shouldPlayCritical && !_isCriticalActive)
            {
                _isCriticalActive = true;
                _criticalSfxHandle = SoundManager.Instance?.PlayLoop(SFXKey.PatientEmergencyBeep);
            }
            else if (!shouldPlayCritical && _isCriticalActive)
            {
                StopCriticalSfx();
            }

            if (health <= 0f && hadPrev && prev > 0f && !_isDeathActive)
            {
                _isDeathActive = true;
                _deathSfxHandle = SoundManager.Instance?.PlayLoop(SFXKey.PatientDeathBeep);
            }
            else if (health > 0f && _isDeathActive)
            {
                StopDeathSfx();
            }
        }

        private void StopCriticalSfx()
        {
            if (_criticalSfxHandle == null)
            {
                return;
            }

            SoundManager.Instance?.StopSFX(_criticalSfxHandle);
            _criticalSfxHandle = null;
            _isCriticalActive = false;
        }

        private void StopDeathSfx()
        {
            if (_deathSfxHandle == null)
            {
                return;
            }

            SoundManager.Instance?.StopSFX(_deathSfxHandle);
            _deathSfxHandle = null;
            _isDeathActive = false;
        }
    }
}
