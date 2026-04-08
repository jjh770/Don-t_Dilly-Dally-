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
        [Tooltip("정답 시 슬롯 최대 확대 비율 (0.8이면 원래 크기의 180%까지)")]
        [SerializeField] private float _punchScale = 0.8f;
        [Tooltip("정답 펀치 전체 지속 시간 (초)")]
        [SerializeField] private float _punchDuration = 0.4f;
        [SerializeField] private float _shakeStrength = 8f;
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private float _pulseDuration = 0.5f;
        [SerializeField] private float _pulseScale = 0.15f;

        private Tween _pulseTween;
        private Tween _fadeTween;
        private ESlotState _state;
        private Vector3 _originalScale;

        public ESlotState State => _state;

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
            // 펄스/이전 스케일 트윈을 완전히 정리하고 원래 크기로 강제 복구
            // (호버링 펄스가 켜져 있어도 간섭 없이 딱! 펀치가 되도록)
            StopPulse();
            DOTween.Kill(transform);
            transform.localScale = _originalScale;

            _arrowImage.color = _clearedColor;

            // 명시적 스케일 시퀀스: 빠르게 커졌다가 원래 크기로 복귀.
            Vector3 peakScale = _originalScale * (1f + _punchScale);
            float upDuration = _punchDuration * 0.35f;
            float downDuration = _punchDuration * 0.65f;

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(transform.DOScale(peakScale, upDuration).SetEase(Ease.OutBack));
            seq.Append(transform.DOScale(_originalScale, downDuration).SetEase(Ease.InOutQuad));

            // 펀치가 내려오는 동안 빠르게 투명화 → 슬라이드 구간이 거의 안 보이게.
            // Join은 직전 Append와 동시에 시작되도록 함.
            if (_canvasGroup != null)
            {
                seq.Join(_canvasGroup.DOFade(0f, downDuration * 0.7f).SetEase(Ease.OutQuad));
            }

            seq.OnKill(() => { if (this != null) transform.localScale = _originalScale; });
            seq.OnComplete(() => { if (this != null) transform.localScale = _originalScale; });
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
