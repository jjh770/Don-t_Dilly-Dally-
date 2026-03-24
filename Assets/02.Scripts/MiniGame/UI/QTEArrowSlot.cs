using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public enum ESlotState
    {
        Pending,
        Current,
        Cleared,
        Failed
    }

    public sealed class QTEArrowSlot : MonoBehaviour
    {
        [SerializeField] private Image _arrowImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("상태별 색상")]
        [SerializeField] private Color _pendingColor = new Color(0.53f, 0.53f, 0.53f, 1f);
        [SerializeField] private Color _currentColor = new Color(1f, 1f, 0f, 1f);
        [SerializeField] private Color _clearedColor = new Color(0f, 1f, 0f, 1f);
        [SerializeField] private Color _failedColor = new Color(1f, 0f, 0f, 1f);

        [Header("DOTween 설정")]
        [SerializeField] private float _punchScale = 0.3f;
        [SerializeField] private float _punchDuration = 0.25f;
        [SerializeField] private float _shakeStrength = 8f;
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private float _pulseDuration = 0.5f;
        [SerializeField] private float _pulseScale = 0.15f;

        private Tween _pulseTween;
        private Tween _fadeTween;
        private ESlotState _state;
        private Vector3 _originalScale;

        private void Awake()
        {
            _originalScale = transform.localScale;

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        // 알파값을 DOTween으로 부드럽게 전환한다.
        public void FadeAlpha(float targetAlpha, float duration)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            if (_fadeTween != null && _fadeTween.IsActive())
            {
                _fadeTween.Kill();
            }

            _fadeTween = _canvasGroup
                .DOFade(targetAlpha, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        public void SetSprite(Sprite sprite)
        {
            if (_arrowImage != null)
            {
                _arrowImage.sprite = sprite;
            }
        }

        public void SetState(ESlotState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;

            switch (state)
            {
                case ESlotState.Pending:
                    StopPulse();
                    _arrowImage.color = _pendingColor;
                    transform.localScale = _originalScale;
                    break;
                case ESlotState.Current:
                    PlayCurrentPulse();
                    break;
                case ESlotState.Cleared:
                    PlayClearEffect();
                    break;
                case ESlotState.Failed:
                    PlayFailEffect();
                    break;
            }
        }

        public void PlayClearEffect()
        {
            StopPulse();
            _arrowImage.color = _clearedColor;
            transform.localScale = _originalScale;
            transform.DOPunchScale(Vector3.one * _punchScale, _punchDuration, 6, 0.5f)
                .SetUpdate(true);
        }

        public void PlayFailEffect()
        {
            StopPulse();
            _arrowImage.color = _failedColor;
            transform.DOShakePosition(_shakeDuration, _shakeStrength, 20, 90f, false, true)
                .SetUpdate(true);
        }

        public void PlayCurrentPulse()
        {
            StopPulse();
            _arrowImage.color = _currentColor;
            transform.localScale = _originalScale;

            _pulseTween = transform
                .DOScale(_originalScale * (1f + _pulseScale), _pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        public void StopPulse()
        {
            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
                _pulseTween = null;
            }
        }

        public void ResetSlot()
        {
            DOTween.Kill(transform);
            StopPulse();

            if (_fadeTween != null && _fadeTween.IsActive())
            {
                _fadeTween.Kill();
                _fadeTween = null;
            }

            _state = ESlotState.Pending;
            _arrowImage.color = _pendingColor;
            transform.localScale = _originalScale;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
            StopPulse();

            if (_fadeTween != null && _fadeTween.IsActive())
            {
                _fadeTween.Kill();
            }
        }
    }
}
