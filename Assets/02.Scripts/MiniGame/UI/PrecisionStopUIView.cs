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

        [Header("색상")]
        [SerializeField] private Image _targetZoneImage;
        [SerializeField] private Color _defaultZoneColor = new Color(0f, 1f, 0f, 0.4f);
        [SerializeField] private Color _hitColor = Color.green;
        [SerializeField] private Color _missColor = Color.red;

        [Header("결과 피드백")]
        [SerializeField] private TextMeshProUGUI _feedbackText;

        [Header("라운드 표시")]
        [SerializeField] private TextMeshProUGUI _roundText;

        [Header("타이머 (Radial)")]
        [Tooltip("Image Type을 Filled, Fill Method를 Radial 360으로 설정하세요")]
        [SerializeField] private Image _radialTimer;

        private PrecisionStopMiniGame _game;
        private float _gaugeWidth;
        private bool? _lastRoundResult;

        public void Initialize(IMiniGame game)
        {
            _game = game as PrecisionStopMiniGame;
            _lastRoundResult = null;

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
            _radialTimer.fillAmount = _game.RemainingTimeRatio;
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
                {
                    _targetZoneImage.color = _hitColor;
                }
            }
            else if (currentResult == false)
            {
                _feedbackText.text = "MISS";
                _feedbackText.color = _missColor;
                if (_targetZoneImage != null)
                {
                    _targetZoneImage.color = _missColor;
                }
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

        public void ShowResult(bool isSuccess)
        {
            if (_feedbackText == null) return;

            _feedbackText.text = isSuccess ? "SUCCESS!" : "FAIL";
            _feedbackText.color = isSuccess ? _hitColor : _missColor;
        }
    }
}
