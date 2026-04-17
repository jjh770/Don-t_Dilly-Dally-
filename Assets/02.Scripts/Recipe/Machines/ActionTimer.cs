using DontDillyDally.UI;
using System;
using UnityEngine;

namespace DontDillyDally.Data
{
    public class ActionTimer : MonoBehaviour
    {
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private RectTransform _positionRoot;
        [SerializeField] private Transform _worldAnchor;
        [SerializeField] private Vector3 _worldOffset = Vector3.up;
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private bool _followWorldAnchor = true;
        [SerializeField] private bool _lockPositionWhileRunning = true;
        [SerializeField] private RadialTimerView _timerView = new RadialTimerView();
        [SerializeField] private bool _hideWhenIdle = true;
        [SerializeField] private bool _useUnscaledTime = false;

        private const float FillImageAlpha = 0.75f;

        private float _duration;
        private float _elapsed;
        private bool _isRunning;
        private Action _onCompleted;
        private CanvasGroup _canvasGroup;
        private bool _isInitialized;
        private bool _hasLockedWorldPosition;
        private Vector3 _lockedWorldPosition;
        private Camera _cachedTargetCamera;

        public bool IsRunning => _isRunning;
        public float Progress01 => _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
        public float RemainingRatio => 1f - Progress01;

        private void Awake()
        {
            EnsureInitialized();
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
            _hasLockedWorldPosition = false;

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
            CaptureOverlayPosition();

            UpdateVisualState(forceHide: false);
            return true;
        }

        public void Cancel()
        {
            _isRunning = false;
            _elapsed = 0f;
            _onCompleted = null;
            _hasLockedWorldPosition = false;
            UpdateVisualState(forceHide: _hideWhenIdle);
        }

        private void UpdateVisualState(bool forceHide)
        {
            EnsureInitialized();

            if (forceHide)
            {
                SetOverlayVisible(false);
                return;
            }

            if (!UpdateOverlayPosition())
            {
                SetOverlayVisible(false);
                return;
            }

            SetOverlayVisible(true);
            _timerView.SetRatio(RemainingRatio);
            _timerView.SetAlpha(FillImageAlpha);
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            if (_overlayRoot == null)
                _overlayRoot = gameObject;

            if (_positionRoot == null)
                _positionRoot = _overlayRoot.GetComponent<RectTransform>();

            if (_worldAnchor == null)
                _worldAnchor = transform;

            if (_canvasGroup == null)
                _canvasGroup = _overlayRoot.GetComponent<CanvasGroup>();

            _timerView ??= new RadialTimerView();
            _timerView.Initialize();
            _timerView.SetAlpha(FillImageAlpha);
            _isInitialized = true;
        }

        private void SetOverlayVisible(bool visible)
        {
            if (_overlayRoot == null)
                return;

            if (_canvasGroup == null)
            {
                _overlayRoot.SetActive(visible);
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        private void CaptureOverlayPosition()
        {
            if (!_lockPositionWhileRunning)
            {
                _hasLockedWorldPosition = false;
                return;
            }

            _lockedWorldPosition = GetTargetWorldPosition();
            _hasLockedWorldPosition = true;
        }

        private bool UpdateOverlayPosition()
        {
            if (!_followWorldAnchor || _positionRoot == null || _worldAnchor == null)
                return true;

            Camera targetCamera = GetTargetCamera();
            if (targetCamera == null)
                return true;

            Vector3 screenPosition = targetCamera.WorldToScreenPoint(GetTargetWorldPosition());
            if (screenPosition.z < 0f)
            {
                return false;
            }

            _positionRoot.position = screenPosition;
            return true;
        }

        private Vector3 GetTargetWorldPosition()
        {
            if (_hasLockedWorldPosition)
                return _lockedWorldPosition;

            Transform anchor = _worldAnchor != null ? _worldAnchor : transform;
            return anchor.position + _worldOffset;
        }

        private Camera GetTargetCamera()
        {
            if (_targetCamera != null)
                return _targetCamera;

            if (_cachedTargetCamera == null)
                _cachedTargetCamera = Camera.main;

            return _cachedTargetCamera;
        }
    }
}
