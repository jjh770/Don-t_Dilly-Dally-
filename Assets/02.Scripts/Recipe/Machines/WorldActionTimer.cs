using System;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.Data
{
    public class WorldActionTimer : MonoBehaviour
    {
        [SerializeField] private Canvas _worldCanvas;
        [SerializeField] private Image _radialFillImage;
        [SerializeField] private bool _hideWhenIdle = true;
        [SerializeField] private bool _useUnscaledTime = false;

        private float _duration;
        private float _elapsed;
        private bool _isRunning;
        private Action _onCompleted;

        public bool IsRunning => _isRunning;
        public float Progress01 => _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
        public float RemainingRatio => 1f - Progress01;

        private void Awake()
        {
            if (_worldCanvas == null)
                _worldCanvas = GetComponentInChildren<Canvas>(true);

            UpdateVisualState(forceHide: _hideWhenIdle);
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _elapsed += deltaTime;

            UpdateVisualState(forceHide: false);

            if (_elapsed < _duration)
                return;

            _elapsed = _duration;
            _isRunning = false;

            UpdateVisualState(forceHide: _hideWhenIdle);

            Action completed = _onCompleted;
            _onCompleted = null;
            completed?.Invoke();
        }

        public bool TryStart(float duration, Action onCompleted)
        {
            if (_isRunning)
                return false;

            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _isRunning = true;
            _onCompleted = onCompleted;

            UpdateVisualState(forceHide: false);
            return true;
        }

        public void Cancel()
        {
            _isRunning = false;
            _elapsed = 0f;
            _onCompleted = null;
            UpdateVisualState(forceHide: _hideWhenIdle);
        }

        private void UpdateVisualState(bool forceHide)
        {
            if (_worldCanvas != null)
                _worldCanvas.enabled = !forceHide;

            if (_radialFillImage != null)
                _radialFillImage.fillAmount = forceHide ? 0f : RemainingRatio;
        }
    }
}
