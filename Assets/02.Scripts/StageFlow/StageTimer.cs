using Photon.Pun;
using System;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    /// <summary>
    /// 스테이지 타이머입니다.
    /// 실제 남은 시간은 Photon 기준 시각으로 계산해 누적 오차를 줄입니다.
    /// 같은 GameObject 또는 별도 GameObject에 컴포넌트로 부착합니다.
    /// </summary>
    public class StageTimer : MonoBehaviour
    {
        [SerializeField] private float _syncIntervalSec = 1f;

        private float _pausedRemainingTime;
        private float _syncAccumulator;
        private bool _running;
        private bool _expired;
        private double _endTimestamp;

        public float RemainingTime => _running ? CalculateRemainingTime() : _pausedRemainingTime;
        public bool IsRunning => _running;

        public event Action OnExpired;
        public event Action<float> OnSyncTick;

        public void Set(float totalSeconds)
        {
            _pausedRemainingTime = Mathf.Max(0f, totalSeconds);
            _syncAccumulator = 0f;
            _running = false;
            _expired = _pausedRemainingTime <= 0f;
            _endTimestamp = GetCurrentTime() + _pausedRemainingTime;
            Debug.Log($"[StageTimer] 타이머 설정: {_pausedRemainingTime:F1}초");
        }

        public void Resume()
        {
            if (_running || _pausedRemainingTime <= 0f)
            {
                return;
            }

            _endTimestamp = GetCurrentTime() + _pausedRemainingTime;
            _syncAccumulator = 0f;
            _running = true;
            _expired = false;
            Debug.Log("[StageTimer] 타이머 재개");
        }

        public void Pause()
        {
            if (!_running)
            {
                return;
            }

            _pausedRemainingTime = CalculateRemainingTime();
            _syncAccumulator = 0f;
            _running = false;
            Debug.Log($"[StageTimer] 타이머 일시정지: {_pausedRemainingTime:F1}초 남음");
        }

        private void Update()
        {
            if (!_running) return;

            float remainingTime = CalculateRemainingTime();

            if (remainingTime <= 0f)
            {
                _pausedRemainingTime = 0f;
                _syncAccumulator = 0f;
                _running = false;

                if (_expired)
                {
                    return;
                }

                _expired = true;
                Debug.Log("[StageTimer] !! 타이머 만료");
                OnSyncTick?.Invoke(0f);
                OnExpired?.Invoke();
                return;
            }

            _syncAccumulator += Time.deltaTime;
            if (_syncAccumulator >= _syncIntervalSec)
            {
                _syncAccumulator -= _syncIntervalSec;
                OnSyncTick?.Invoke(remainingTime);
            }
        }

        public void ResetTimer()
        {
            _pausedRemainingTime = 0f;
            _syncAccumulator = 0f;
            _running = false;
            _expired = false;
            _endTimestamp = GetCurrentTime();
        }

        private float CalculateRemainingTime()
        {
            return Mathf.Max(0f, (float)(_endTimestamp - GetCurrentTime()));
        }

        private static double GetCurrentTime()
        {
            return PhotonNetwork.IsConnected ? PhotonNetwork.Time : Time.realtimeSinceStartupAsDouble;
        }
    }
}
