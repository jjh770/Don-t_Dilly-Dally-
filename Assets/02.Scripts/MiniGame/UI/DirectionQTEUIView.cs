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
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _failColor = Color.red;

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
            if (_game == null || _game.CurrentState != EMiniGameState.Playing) return;

            UpdateRadialTimer();
            UpdateSequenceHighlight();
            UpdateFeedback();
        }

        public void ShowResult(bool isSuccess)
        {
            if (_feedbackText == null) return;

            _feedbackText.text = isSuccess ? "SUCCESS!" : "FAIL";
            _feedbackText.color = isSuccess ? _successColor : _failColor;
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
                sb.Append(DirectionToArrow(prompts[i].EQteDirection));
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

                string arrow = DirectionToArrow(prompts[i].EQteDirection);
                if (i < _game.CurrentPromptIndex)
                {
                    // 클리어한 방향 - 초록색.
                    sb.Append($"<color=#00FF00>{arrow}</color>");
                }
                else if (i == _game.CurrentPromptIndex)
                {
                    // 현재 입력해야 할 방향 - 노란색 + 굵게.
                    sb.Append($"<color=#FFFF00><b>{arrow}</b></color>");
                }
                else
                {
                    // 아직 안 온 방향 - 회색.
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
                _feedbackText.color = _successColor;
            }
            else if (currentResult == false)
            {
                _feedbackText.text = "MISS";
                _feedbackText.color = _failColor;
            }
        }

        private static string DirectionToArrow(EQteDirection dir)
        {
            return dir switch
            {
                EQteDirection.Up => "\u2191",
                EQteDirection.Down => "\u2193",
                EQteDirection.Left => "\u2190",
                EQteDirection.Right => "\u2192",
                _ => ""
            };
        }
    }
}
