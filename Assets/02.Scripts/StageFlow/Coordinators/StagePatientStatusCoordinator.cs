using System;

namespace DontDillyDally.StageFlow
{
    // 현재 환자의 체력 초기화, 회복, 자연 감소를 전담
    public sealed class StagePatientStatusCoordinator : IDisposable
    {
        private const float CRITICAL_HEALTH_THRESHOLD = 0.3f;

        private readonly PatientHealthController _controller;
        private readonly StageFlowRpcHandler _rpc;
        private readonly Action<EGameOverReason> _onHealthDepleted;

        private float _maxHealth;
        private bool _criticalEventTriggered;

        public StagePatientStatusCoordinator(
            PatientHealthController controller,
            StageFlowRpcHandler rpc,
            Action<EGameOverReason> onHealthDepleted)
        {
            _controller = controller;
            _rpc = rpc;
            _onHealthDepleted = onHealthDepleted;

            if (_controller != null)
            {
                _controller.OnHealthChanged += HandleHealthChanged;
                _controller.OnHealthDepleted += HandleHealthDepleted;
            }
        }

        // ── 환자 상태 제어 ───────────────────────────────────────────

        public float CurrentHealth => _controller?.CurrentHealth ?? 0f;

        public void Initialize(float maxHealth, float drainPerSecond, EStagePhase currentPhase)
        {
            if (_controller == null)
            {
                return;
            }

            _maxHealth = maxHealth;
            _criticalEventTriggered = false;
            _controller.Initialize(maxHealth, drainPerSecond);

            if (currentPhase == EStagePhase.Playing)
            {
                _controller.ResumeDrain();
            }
        }

        public float ApplyDamage(float damage)
        {
            if (_controller == null)
            {
                return _rpc != null ? _rpc.PatientHealth.Value : 0f;
            }

            _controller.ApplyDamage(damage);
            return _controller.CurrentHealth;
        }

        public float ApplyHeal(float heal)
        {
            if (_controller == null)
            {
                return _rpc != null ? _rpc.PatientHealth.Value : 0f;
            }

            _controller.ApplyHeal(heal);
            return _controller.CurrentHealth;
        }

        public void PauseDrain()
        {
            _controller?.PauseDrain();
        }

        public void ResumeDrain(EStagePhase currentPhase)
        {
            if (currentPhase == EStagePhase.Playing)
            {
                _controller?.ResumeDrain();
            }
        }

        public void Tick(float deltaTime)
        {
            _controller?.Tick(deltaTime);
        }

        // ── 정리 ────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_controller == null)
            {
                return;
            }

            _controller.OnHealthChanged -= HandleHealthChanged;
            _controller.OnHealthDepleted -= HandleHealthDepleted;
            _controller.Reset();
        }

        // ── 내부 이벤트 전달 ─────────────────────────────────────────

        private void HandleHealthChanged(float health)
        {
            _rpc?.SetHealth(health);

            // 체력이 30% 미만이면 PatientCritical 이벤트 발생 (한 번만)
            if (!_criticalEventTriggered && _maxHealth > 0f && health / _maxHealth < CRITICAL_HEALTH_THRESHOLD)
            {
                _criticalEventTriggered = true;
                EventManager.Instance?.OnPatientCritical();
            }
            // 체력이 회복되면 플래그 리셋
            else if (_criticalEventTriggered && _maxHealth > 0f && health / _maxHealth >= CRITICAL_HEALTH_THRESHOLD)
            {
                _criticalEventTriggered = false;
            }
        }

        private void HandleHealthDepleted()
        {
            _onHealthDepleted?.Invoke(EGameOverReason.PatientDeath);
        }
    }
}
