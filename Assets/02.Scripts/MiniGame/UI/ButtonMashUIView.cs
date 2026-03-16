using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class ButtonMashUIView : MonoBehaviour, IMiniGameUIView
    {
        [Header("게이지")]
        [SerializeField] private Image _gaugeBarFill;
        [SerializeField] private RectTransform _successLineMarker;

        [Header("색상")]
        [SerializeField] private Color _belowThresholdColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color _aboveThresholdColor = Color.green;

        [Header("타이머")]
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("입력 안내")]
        [SerializeField] private RectTransform _keyIcon;

        private ButtonMashMiniGame _game;
        private ButtonMashConfig _config;
        private float _keyIconBaseScale;

        public void Initialize(IMiniGame game)
        {
            _game = game as ButtonMashMiniGame;
            _config = null;
            if (_keyIcon != null)
            {
                _keyIconBaseScale = _keyIcon.localScale.x;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void UpdateView()
        {
            if (_game == null || _game.CurrentState != MiniGameState.Playing) return;

            // 게이지 바 업데이트
            float progress = _game.NormalizedProgress;
            _gaugeBarFill.fillAmount = progress;

            // 게이지 색상
            if (_config != null)
            {
                _gaugeBarFill.color = progress >= _config.successThreshold
                    ? _aboveThresholdColor
                    : _belowThresholdColor;
            }
            else
            {
                _gaugeBarFill.color = progress >= 0.8f
                    ? _aboveThresholdColor
                    : _belowThresholdColor;
            }

            // 타이머
            if (_timerText != null && _config != null)
            {
                float remaining = Mathf.Max(0f, _config.timeLimit - Time.time);
                _timerText.text = $"{remaining:F1}";
            }
        }

        public void SetConfig(ButtonMashConfig config)
        {
            _config = config;

            // 성공 라인 위치 설정
            if (_successLineMarker != null && _config != null)
            {
                float anchorY = _config.successThreshold;
                _successLineMarker.anchorMin = new Vector2(0f, anchorY);
                _successLineMarker.anchorMax = new Vector2(1f, anchorY);
                _successLineMarker.anchoredPosition = Vector2.zero;
            }
        }
    }
}
