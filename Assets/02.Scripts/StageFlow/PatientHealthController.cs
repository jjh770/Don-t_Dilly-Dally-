using System;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    /// <summary>
    /// 현재 환자의 체력과 자연 감소를 전담합니다.
    /// StageFlowManager는 시작/정지/피해 적용만 요청하고,
    /// 실제 체력 계산과 사망 판정은 이 컨트롤러가 맡습니다.
    /// </summary>
    public class PatientHealthController
    {
        private float _currentHealth;
        private float _maxHealth = 1f;
        private float _drainPerSecond;
        private bool _isInitialized;
        private bool _isDrainActive;
        private bool _isDepleted;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float DrainPerSecond => _drainPerSecond;
        public bool IsDrainActive => _isDrainActive;
        public bool IsInitialized => _isInitialized;

        public event Action<float> OnHealthChanged;
        public event Action OnHealthDepleted;

        public void Initialize(float maxHealth, float drainPerSecond)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);
            _currentHealth = _maxHealth;
            _drainPerSecond = Mathf.Max(0f, drainPerSecond);
            _isInitialized = true;
            _isDrainActive = false;
            _isDepleted = false;

            OnHealthChanged?.Invoke(_currentHealth);
        }

        public void ResumeDrain()
        {
            if (!_isInitialized || _isDepleted || _currentHealth <= 0f)
            {
                return;
            }

            _isDrainActive = true;
        }

        public void PauseDrain()
        {
            _isDrainActive = false;
        }

        public void Tick(float deltaTime)
        {
            if (!_isInitialized ||
                !_isDrainActive ||
                _isDepleted ||
                deltaTime <= 0f ||
                _drainPerSecond <= 0f)
            {
                return;
            }

            ApplyDamageInternal(_drainPerSecond * deltaTime);
        }

        public void ApplyDamage(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            ApplyDamageInternal(damage);
        }

        public void ApplyHeal(float heal)
        {
            if (!_isInitialized || _isDepleted || heal <= 0f)
            {
                return;
            }

            float nextHealth = Mathf.Min(_maxHealth, _currentHealth + heal);
            if (Mathf.Approximately(nextHealth, _currentHealth))
            {
                return;
            }

            _currentHealth = nextHealth;
            OnHealthChanged?.Invoke(_currentHealth);
        }

        public void Reset()
        {
            _currentHealth = 0f;
            _maxHealth = 1f;
            _drainPerSecond = 0f;
            _isInitialized = false;
            _isDrainActive = false;
            _isDepleted = false;
        }

        private void ApplyDamageInternal(float damage)
        {
            if (!_isInitialized || _isDepleted)
            {
                return;
            }

            float nextHealth = Mathf.Max(0f, _currentHealth - damage);
            if (Mathf.Approximately(nextHealth, _currentHealth))
            {
                return;
            }

            _currentHealth = nextHealth;
            OnHealthChanged?.Invoke(_currentHealth);

            if (_currentHealth > 0f || _isDepleted)
            {
                return;
            }

            _isDepleted = true;
            _isDrainActive = false;
            OnHealthDepleted?.Invoke();
        }
    }
}
