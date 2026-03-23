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

        [Header("타이머 그라디언트 색상")]
        [SerializeField] private Color _timerColorFull = new Color(0.4f, 1f, 0.2f);     // 연두색 (시간 충분)
        [SerializeField] private Color _timerColorMid = new Color(1f, 0.6f, 0f);        // 주황색 (절반)
        [SerializeField] private Color _timerColorEmpty = new Color(1f, 0.2f, 0.2f);    // 빨간색 (시간 부족)

        [Header("스페이스바 힌트 애니메이션")]
        [Tooltip("기본 상태 스페이스바")]
        [SerializeField] private GameObject _spaceBarIdle;
        [Tooltip("눌린 상태 스페이스바")]
        [SerializeField] private GameObject _spaceBarPressed;
        [Tooltip("눌린 상태 이펙트")]
        [SerializeField] private GameObject _spaceBarEffect;
        [Tooltip("Idle ↔ Pressed 전환 간격(초)")]
        [SerializeField, Range(0.05f, 2f)] private float _hintInterval = 0.45f;

        [Header("공통 결과 연출")]
        [SerializeField] private MiniGameResultEffect _resultEffect;

        private ButtonMashMiniGame _game;
        private float _hintTimer;
        private bool _hintShowPressed;

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
                _radialTimer.color = _timerColorFull;
            }

            if (_resultEffect != null)
            {
                _resultEffect.Reset();
            }

            // 힌트 애니메이션 초기화 — Idle 상태로 시작
            _hintTimer = 0f;
            _hintShowPressed = false;
            SetSpaceBarHint(pressed: false);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void UpdateView()
        {
            if (_game == null || _game.CurrentState != EMiniGameState.Playing)
            {
                return;
            }

            // 게이지 바 업데이트
            if (_gaugeBarFill != null)
            {
                float progress = _game.NormalizedProgress;
                _gaugeBarFill.fillAmount = progress;
                _gaugeBarFill.color = EvaluateGaugeColor(progress);
            }

            // Radial 타이머 (Game 로직에서 직접 비율을 가져옴)
            if (_radialTimer != null)
            {
                float timeRatio = _game.RemainingTimeRatio;
                _radialTimer.fillAmount = timeRatio;
                _radialTimer.color = EvaluateTimerColor(timeRatio);
            }

            // 스페이스바 힌트 반복 토글
            _hintTimer += Time.deltaTime;
            if (_hintTimer >= _hintInterval)
            {
                _hintTimer -= _hintInterval;
                _hintShowPressed = !_hintShowPressed;
                SetSpaceBarHint(_hintShowPressed);
            }
        }

        public void ShowResult(bool isSuccess)
        {
            // 성공 시 게이지를 꽉 찬 상태로 보정
            if (isSuccess && _gaugeBarFill != null)
            {
                _gaugeBarFill.fillAmount = 1f;
                _gaugeBarFill.color = _colorFull;
            }

            if (_resultEffect != null)
            {
                if (isSuccess)
                {
                    _resultEffect.PlaySuccess();
                }
                else
                {
                    _resultEffect.PlayFail();
                }
            }
        }

        // 0~1 남은 시간 비율을 연두 → 주황 → 빨강 그라디언트로 변환.
        private Color EvaluateTimerColor(float t)
        {
            if (t >= 0.5f)
            {
                // 1.0~0.5 구간: 연두 → 주황
                return Color.Lerp(_timerColorMid, _timerColorFull, (t - 0.5f) * 2f);
            }
            else
            {
                // 0.5~0.0 구간: 주황 → 빨강
                return Color.Lerp(_timerColorEmpty, _timerColorMid, t * 2f);
            }
        }

        // Idle(1번) ↔ Pressed+Effect(2,3번) 토글.
        private void SetSpaceBarHint(bool pressed)
        {
            if (_spaceBarIdle != null)
            {
                _spaceBarIdle.SetActive(!pressed);
            }

            if (_spaceBarPressed != null)
            {
                _spaceBarPressed.SetActive(pressed);
            }

            if (_spaceBarEffect != null)
            {
                _spaceBarEffect.SetActive(pressed);
            }
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
