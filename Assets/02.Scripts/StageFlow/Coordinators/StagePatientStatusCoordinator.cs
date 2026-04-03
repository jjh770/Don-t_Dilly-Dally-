using System;

namespace DontDillyDally.StageFlow
{
    // 현재 환자의 체력 초기화, 회복, 자연 감소를 전담합니다.
    public sealed class StagePatientStatusCoordinator : IDisposable
    {
        private readonly PatientHealthController _controller;
        private readonly StageFlowRpcHandler _rpc;
        private readonly Action<EGameOverReason> _onHealthDepleted;

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
        }

        private void HandleHealthDepleted()
        {
            _onHealthDepleted?.Invoke(EGameOverReason.PatientDeath);
        }
    }
}
