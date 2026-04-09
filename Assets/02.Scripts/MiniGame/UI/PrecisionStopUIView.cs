using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class PrecisionStopUIView : MiniGameUIViewBase<PrecisionStopMiniGame>
    {
        [Header("게이지 바")]
        [SerializeField] private RectTransform _gaugeBar;
        [SerializeField] private RectTransform _targetZone;
        [SerializeField] private RectTransform _cursor;

        [Header("히트존 색상")]
        [SerializeField] private Image _targetZoneImage;
        [SerializeField] private Color _defaultZoneColor = new Color(0f, 1f, 0f, 0.75f);
        [SerializeField] private Color _hitColor = Color.green;
        [SerializeField] private Color _missColor = Color.red;

        [Header("라운드 중간 피드백")]
        [SerializeField] private TextMeshProUGUI _feedbackText;

        [Header("라운드 표시")]
        [SerializeField] private TextMeshProUGUI _roundText;

        [Header("타이머 (Radial)")]
        [SerializeField] private RadialTimerView _timer;

        [Header("DOTween 라운드 피드백")]
        [Tooltip("흔들림 대상 (미할당 시 게이지 바 사용)")]
        [SerializeField] private RectTransform _shakeTarget;

        [Tooltip("성공(GREAT!) — 작은 펀치 스케일")]
        [SerializeField] private float _hitPunchScale = 0.08f;
        [SerializeField] private float _hitPunchDuration = 0.25f;

        [Tooltip("실패(MISS) — 큰 흔들림")]
        [SerializeField] private float _missShakeStrength = 12f;
        [SerializeField] private float _missShakeDuration = 0.35f;

        private float _gaugeWidth;
        private bool? _lastRoundResult;
        private Tween _shakeTween;

        protected override void OnInitialize()
        {
            _lastRoundResult = null;

            KillShakeTween();

            if (_gaugeBar != null)
            {
                _gaugeWidth = _gaugeBar.rect.width;
            }

            if (_feedbackText != null)
            {
                _feedbackText.text = "";
            }

            if (_targetZoneImage != null)
            {
                _targetZoneImage.color = _defaultZoneColor;
            }

            _timer.Initialize();
        }

        public override void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public override void UpdateView()
        {
            if (Game == null)
            {
                return;
            }

            UpdateCursorPosition();
            UpdateTargetZone();
            _timer.SetRatio(Game.RemainingTimeRatio);
            UpdateRoundText();
            UpdateFeedback();
        }

        private void UpdateCursorPosition()
        {
            if (_cursor == null || _gaugeBar == null)
            {
                return;
            }

            float xPos = Game.CursorPosition * _gaugeWidth - _gaugeWidth * 0.5f;
            _cursor.anchoredPosition = new Vector2(xPos, _cursor.anchoredPosition.y);
        }

        private void UpdateTargetZone()
        {
            if (_targetZone == null || _gaugeBar == null)
            {
                return;
            }

            float center = Game.TargetZoneCenter;
            float width = Game.TargetZoneWidth;

            float left = (center - width * 0.5f) * _gaugeWidth - _gaugeWidth * 0.5f;
            float zonePixelWidth = width * _gaugeWidth;

            _targetZone.anchoredPosition = new Vector2(left + zonePixelWidth * 0.5f, _targetZone.anchoredPosition.y);
            _targetZone.sizeDelta = new Vector2(zonePixelWidth, _targetZone.sizeDelta.y);
        }

        private void UpdateRoundText()
        {
            if (_roundText == null)
            {
                return;
            }

            _roundText.text = $"{Game.DisplayRound} / {Game.TotalRounds}";
        }

        private void UpdateFeedback()
        {
            if (_feedbackText == null)
            {
                return;
            }

            bool? currentResult = Game.LastRoundResult;
            if (currentResult == _lastRoundResult)
            {
                return;
            }

            _lastRoundResult = currentResult;

            if (currentResult == true)
            {
                _feedbackText.text = "GREAT!";
                _feedbackText.color = _hitColor;

                if (_targetZoneImage != null)
                {
                    _targetZoneImage.color = _hitColor;
                }

                PlayHitShake();
            }
            else if (currentResult == false)
            {
                _feedbackText.text = "MISS";
                _feedbackText.color = _missColor;

                if (_targetZoneImage != null)
                {
                    _targetZoneImage.color = _missColor;
                }

                PlayMissShake();
            }
            else
            {
                _feedbackText.text = "";

                if (_targetZoneImage != null)
                {
                    _targetZoneImage.color = _defaultZoneColor;
                }
            }
        }

        // ── DOTween 라운드 피드백 ──

        private RectTransform ShakeTarget =>
            _shakeTarget != null ? _shakeTarget : _gaugeBar;

        private void PlayHitShake()
        {
            RectTransform target = ShakeTarget;
            if (target == null)
            {
                return;
            }

            KillShakeTween();
            target.localScale = Vector3.one;

            _shakeTween = target
                .DOPunchScale(Vector3.one * _hitPunchScale, _hitPunchDuration, 8, 0.4f)
                .SetUpdate(true);
        }

        private void PlayMissShake()
        {
            RectTransform target = ShakeTarget;
            if (target == null)
            {
                return;
            }

            KillShakeTween();

            _shakeTween = target
                .DOShakePosition(_missShakeDuration, _missShakeStrength, 20, 90f, false, true)
                .SetUpdate(true);
        }

        private void KillShakeTween()
        {
            if (_shakeTween != null && _shakeTween.IsActive())
            {
                _shakeTween.Kill();
                _shakeTween = null;
            }
        }

        private void OnDestroy()
        {
            KillShakeTween();
        }
    }
}
