using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class DirectionQTEUIView : MonoBehaviour, IMiniGameUIView
    {
        [Header("현재 방향 표시")]
        [SerializeField] private TextMeshProUGUI _directionText;
        [SerializeField] private Image _radialTimer;

        [Header("결과 피드백")]
        [SerializeField] private TextMeshProUGUI _feedbackText;

        [Header("시퀀스 진행 표시")]
        [SerializeField] private Image[] _sequenceDots;
        [SerializeField] private Color _successDotColor = Color.green;
        [SerializeField] private Color _failDotColor = Color.red;
        [SerializeField] private Color _pendingDotColor = Color.gray;
        [SerializeField] private Color _currentDotColor = Color.yellow;

        [Header("실수 허용 표시")]
        [SerializeField] private Image[] _mistakeHearts;

        private DirectionQTEMiniGame _game;
        private int _lastPromptIndex = -1;
        private bool? _lastInputResult;

        public void Initialize(IMiniGame game)
        {
            _game = game as DirectionQTEMiniGame;
            _lastPromptIndex = -1;
            _lastInputResult = null;

            if (_feedbackText != null)
            {
                _feedbackText.text = "";
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void UpdateView()
        {
            if (_game == null || _game.CurrentState != MiniGameState.Playing) return;

            UpdateDirectionDisplay();
            UpdateRadialTimer();
            UpdateSequenceDots();
            UpdateFeedback();
        }

        private void UpdateDirectionDisplay()
        {
            Direction? dir = _game.CurrentDirection;
            if (dir == null || _directionText == null) return;

            _directionText.text = dir.Value switch
            {
                Direction.Up => "\u2191",
                Direction.Down => "\u2193",
                Direction.Left => "\u2190",
                Direction.Right => "\u2192",
                _ => ""
            };
        }

        private void UpdateRadialTimer()
        {
            if (_radialTimer == null) return;
            _radialTimer.fillAmount = _game.CurrentPromptRemainingRatio;
        }

        private void UpdateSequenceDots()
        {
            if (_sequenceDots == null) return;

            for (int i = 0; i < _sequenceDots.Length && i < _game.TotalPrompts; i++)
            {
                if (_sequenceDots[i] == null) continue;

                if (i < _game.CurrentPromptIndex)
                {
                    // 이미 지나간 프롬프트
                    _sequenceDots[i].color = _successDotColor;
                }
                else if (i == _game.CurrentPromptIndex)
                {
                    _sequenceDots[i].color = _currentDotColor;
                }
                else
                {
                    _sequenceDots[i].color = _pendingDotColor;
                }
            }
        }

        private void UpdateFeedback()
        {
            if (_feedbackText == null) return;

            bool? currentResult = _game.LastInputResult;
            if (currentResult == _lastInputResult && _game.CurrentPromptIndex == _lastPromptIndex) return;

            _lastInputResult = currentResult;
            _lastPromptIndex = _game.CurrentPromptIndex;

            if (currentResult == true)
            {
                _feedbackText.text = "GOOD!";
                _feedbackText.color = _successDotColor;
            }
            else if (currentResult == false)
            {
                _feedbackText.text = "MISS";
                _feedbackText.color = _failDotColor;
            }
        }
    }
}
