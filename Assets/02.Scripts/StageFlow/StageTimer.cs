using System;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    /// <summary>
    /// 스테이지 타이머. 자체 Update()로 카운트다운합니다.
    /// 같은 GameObject 또는 별도 GameObject에 컴포넌트로 부착합니다.
    /// </summary>
    public class StageTimer : MonoBehaviour
    {
        [SerializeField] private float _syncIntervalSec = 1f;

        private float _remainingTime;
        private float _syncAccumulator;
        private bool _running;

        public float RemainingTime => _remainingTime;
        public bool IsRunning => _running;

        public event Action OnExpired;
        public event Action<float> OnSyncTick;

        public void Set(float totalSeconds)
        {
            _remainingTime = totalSeconds;
            _syncAccumulator = 0f;
            Debug.Log($"[StageTimer] 타이머 설정: {totalSeconds}초");
        }

        public void Resume()
        {
            _running = true;
            Debug.Log("[StageTimer] 타이머 재개");
        }

        public void Pause()
        {
            _running = false;
            Debug.Log("[StageTimer] 타이머 일시정지");
        }

        private void Update()
        {
            if (!_running) return;

            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                _running = false;
                Debug.Log("[StageTimer] !! 타이머 만료");
                OnExpired?.Invoke();
                return;
            }

            _syncAccumulator += Time.deltaTime;
            if (_syncAccumulator >= _syncIntervalSec)
            {
                _syncAccumulator = 0f;
                OnSyncTick?.Invoke(_remainingTime);
            }
        }

        public void ResetTimer()
        {
            _remainingTime = 0f;
            _syncAccumulator = 0f;
            _running = false;
        }
    }
}
