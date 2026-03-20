using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class ButtonMashUIView : MonoBehaviour, IMiniGameUIView
    {
        [Header("게이지")]
        [SerializeField] private Image _gaugeBarFill;

        [Header("게이지 그라디언트 색상")]
        [SerializeField] private Color _colorEmpty = new Color(1f, 0.2f, 0.2f);    // 빨간색 (0%)
        [SerializeField] private Color _colorMid = new Color(1f, 0.6f, 0f);        // 주황색 (50%)
        [SerializeField] private Color _colorFull = new Color(0.4f, 1f, 0.2f);     // 연두색 (100%)

        [Header("타이머 (Radial)")]
        [Tooltip("Image Type을 Filled, Fill Method를 Radial 360으로 설정하세요")]
        [SerializeField] private Image _radialTimer;

        [Header("공통 결과 연출")]
        [SerializeField] private MiniGameResultEffect _resultEffect;

        private ButtonMashMiniGame _game;

        public void Initialize(IMiniGame game)
        {
            _game = game as ButtonMashMiniGame;

            if (_gaugeBarFill != null)
            {
                _gaugeBarFill.fillAmount = 0f;
                _gaugeBarFill.color = _colorEmpty;
            }

            if (_radialTimer != null)
            {
                _radialTimer.fillAmount = 1f;
            }

            if (_resultEffect != null)
            {
                _resultEffect.Reset();
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void UpdateView()
        {
            if (_game == null || _game.CurrentState != EMiniGameState.Playing) return;

            // 게이지 바 업데이트
            float progress = _game.NormalizedProgress;
            _gaugeBarFill.fillAmount = progress;

            // 게이지 색상 그라디언트: 빨강(0%) → 주황(50%) → 연두(100%)
            _gaugeBarFill.color = EvaluateGaugeColor(progress);

            // Radial 타이머 (Game 로직에서 직접 비율을 가져옴)
            if (_radialTimer != null)
            {
                _radialTimer.fillAmount = _game.RemainingTimeRatio;
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

        // 0~1 progress를 빨강 → 주황 → 연두 그라디언트로 변환.
        private Color EvaluateGaugeColor(float t)
        {
            if (t <= 0.5f)
            {
                // 0~0.5 구간: 빨강 → 주황
                return Color.Lerp(_colorEmpty, _colorMid, t * 2f);
            }
            else
            {
                // 0.5~1.0 구간: 주황 → 연두
                return Color.Lerp(_colorMid, _colorFull, (t - 0.5f) * 2f);
            }
        }
    }
}
