using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class PrecisionStopUIView : MonoBehaviour, IMiniGameUIView
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

        [Header("공통 결과 연출")]
        [SerializeField] private MiniGameResultEffect _resultEffect;

        [Header("라운드 표시")]
        [SerializeField] private TextMeshProUGUI _roundText;

        [Header("타이머 (Radial)")]
        [Tooltip("Image Type을 Filled, Fill Method를 Radial 360으로 설정하세요")]
        [SerializeField] private Image _radialTimer;

        [Header("타이머 그라디언트 색상")]
        [SerializeField] private Color _timerColorFull = new Color(0.4f, 1f, 0.2f);
        [SerializeField] private Color _timerColorMid = new Color(1f, 0.6f, 0f);
        [SerializeField] private Color _timerColorEmpty = new Color(1f, 0.2f, 0.2f);

        [Header("DOTween 라운드 피드백")]
        [Tooltip("흔들림 대상 (미할당 시 게이지 바 사용)")]
        [SerializeField] private RectTransform _shakeTarget;

        [Tooltip("성공(GREAT!) — 작은 펀치 스케일")]
        [SerializeField] private float _hitPunchScale = 0.08f;
        [SerializeField] private float _hitPunchDuration = 0.25f;

        [Tooltip("실패(MISS) — 큰 흔들림")]
        [SerializeField] private float _missShakeStrength = 12f;
        [SerializeField] private float _missShakeDuration = 0.35f;

        private PrecisionStopMiniGame _game;
        private float _gaugeWidth;
        private bool? _lastRoundResult;
        private Tween _shakeTween;

        public void Initialize(IMiniGame game)
        {
            _game = game as PrecisionStopMiniGame;
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

            if (_radialTimer != null)
            {
                _radialTimer.fillAmount = 1f;
                _radialTimer.color = _timerColorFull;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void UpdateView()
        {
            if (_game == null) return;

            UpdateCursorPosition();
            UpdateTargetZone();
            UpdateRadialTimer();
            UpdateRoundText();
            UpdateFeedback();
        }

        private void UpdateCursorPosition()
        {
            if (_cursor == null || _gaugeBar == null) return;

            float xPos = _game.CursorPosition * _gaugeWidth - _gaugeWidth * 0.5f;
            _cursor.anchoredPosition = new Vector2(xPos, _cursor.anchoredPosition.y);
        }

        private void UpdateTargetZone()
        {
            if (_targetZone == null || _gaugeBar == null) return;

            float center = _game.TargetZoneCenter;
            float width = _game.TargetZoneWidth;

            float left = (center - width * 0.5f) * _gaugeWidth - _gaugeWidth * 0.5f;
            float zonePixelWidth = width * _gaugeWidth;

            _targetZone.anchoredPosition = new Vector2(left + zonePixelWidth * 0.5f, _targetZone.anchoredPosition.y);
            _targetZone.sizeDelta = new Vector2(zonePixelWidth, _targetZone.sizeDelta.y);
        }

        private void UpdateRadialTimer()
        {
            if (_radialTimer == null) return;

            float timeRatio = _game.RemainingTimeRatio;
            _radialTimer.fillAmount = timeRatio;
            _radialTimer.color = EvaluateTimerColor(timeRatio);
        }

        private void UpdateRoundText()
        {
            if (_roundText == null) return;
            _roundText.text = $"{_game.DisplayRound} / {_game.TotalRounds}";
        }

        private void UpdateFeedback()
        {
            if (_feedbackText == null) return;

            bool? currentResult = _game.LastRoundResult;
            if (currentResult == _lastRoundResult) return;

            _lastRoundResult = currentResult;

            if (currentResult == true)
            {
                _feedbackText.text = "GREAT!";
                _feedbackText.color = _hitColor;

                if (_targetZoneImage != null)
                    _targetZoneImage.color = _hitColor;

                PlayHitShake();
            }
            else if (currentResult == false)
            {
                _feedbackText.text = "MISS";
                _feedbackText.color = _missColor;

                if (_targetZoneImage != null)
                    _targetZoneImage.color = _missColor;

                PlayMissShake();
            }
            else
            {
                _feedbackText.text = "";

                if (_targetZoneImage != null)
                    _targetZoneImage.color = _defaultZoneColor;
            }
        }

        public void ShowResult(bool isSuccess)
        {
            if (_resultEffect == null) return;

            if (isSuccess)
                _resultEffect.PlaySuccess();
            else
                _resultEffect.PlayFail();
        }

        // ── DOTween 라운드 피드백 ──

        private RectTransform ShakeTarget =>
            _shakeTarget != null ? _shakeTarget : _gaugeBar;

        private void PlayHitShake()
        {
            RectTransform target = ShakeTarget;
            if (target == null) return;

            KillShakeTween();
            target.localScale = Vector3.one;

            _shakeTween = target
                .DOPunchScale(Vector3.one * _hitPunchScale, _hitPunchDuration, 8, 0.4f)
                .SetUpdate(true);
        }

        private void PlayMissShake()
        {
            RectTransform target = ShakeTarget;
            if (target == null) return;

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

        // ── 타이머 그라디언트 ──

        private Color EvaluateTimerColor(float t)
        {
            if (t >= 0.5f)
                return Color.Lerp(_timerColorMid, _timerColorFull, (t - 0.5f) * 2f);
            else
                return Color.Lerp(_timerColorEmpty, _timerColorMid, t * 2f);
        }

        private void OnDestroy()
        {
            KillShakeTween();
        }
    }
}
