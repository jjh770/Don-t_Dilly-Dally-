using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class DirectionQTEUIView : MonoBehaviour, IMiniGameUIView
    {
        [Header("전체 시퀀스 표시")]
        [SerializeField] private TextMeshProUGUI _sequenceText;
        [SerializeField] private Image _radialTimer;

        [Header("결과 피드백")]
        [SerializeField] private TextMeshProUGUI _feedbackText;

        [Header("시퀀스 진행 표시")]
        [SerializeField] private Image[] _sequenceDots;
        [SerializeField] private Color _successDotColor = Color.green;
        [SerializeField] private Color _failDotColor = Color.red;
        [SerializeField] private Color _pendingDotColor = Color.gray;
        [SerializeField] private Color _currentDotColor = Color.yellow;

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

            BuildSequenceDisplay();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void UpdateView()
        {
            if (_game == null || _game.CurrentState != MiniGameState.Playing) return;

            UpdateRadialTimer();
            UpdateSequenceDots();
            UpdateSequenceHighlight();
            UpdateFeedback();
        }

        private void BuildSequenceDisplay()
        {
            if (_sequenceText == null || _game == null) return;

            var prompts = _game.Prompts;
            if (prompts == null) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < prompts.Length; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(DirectionToArrow(prompts[i].Direction));
            }
            _sequenceText.text = sb.ToString();
        }

        private void UpdateSequenceHighlight()
        {
            if (_sequenceText == null || _game == null) return;

            var prompts = _game.Prompts;
            if (prompts == null) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < prompts.Length; i++)
            {
                if (i > 0) sb.Append("  ");

                string arrow = DirectionToArrow(prompts[i].Direction);
                if (i < _game.CurrentPromptIndex)
                {
                    // 클리어한 방향 - 초록색
                    sb.Append($"<color=#00FF00>{arrow}</color>");
                }
                else if (i == _game.CurrentPromptIndex)
                {
                    // 현재 입력해야 할 방향 - 노란색 + 굵게
                    sb.Append($"<color=#FFFF00><b>{arrow}</b></color>");
                }
                else
                {
                    // 아직 안 온 방향 - 회색
                    sb.Append($"<color=#888888>{arrow}</color>");
                }
            }
            _sequenceText.text = sb.ToString();
        }

        private void UpdateRadialTimer()
        {
            if (_radialTimer == null) return;
            _radialTimer.fillAmount = _game.RemainingTimeRatio;
        }

        private void UpdateSequenceDots()
        {
            if (_sequenceDots == null) return;

            for (int i = 0; i < _sequenceDots.Length && i < _game.TotalPrompts; i++)
            {
                if (_sequenceDots[i] == null) continue;

                if (i < _game.CurrentPromptIndex)
                {
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

        public void ShowResult(bool isSuccess)
        {
            if (_feedbackText == null) return;

            _feedbackText.text = isSuccess ? "SUCCESS!" : "FAIL";
            _feedbackText.color = isSuccess ? _successDotColor : _failDotColor;
        }

        private static string DirectionToArrow(Direction dir)
        {
            return dir switch
            {
                Direction.Up => "\u2191",
                Direction.Down => "\u2193",
                Direction.Left => "\u2190",
                Direction.Right => "\u2192",
                _ => ""
            };
        }
    }
}
