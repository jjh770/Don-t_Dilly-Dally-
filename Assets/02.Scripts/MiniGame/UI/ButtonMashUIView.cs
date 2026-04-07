using DG.Tweening;
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

        [Header("충격 흔들림 연출 (DOTween)")]
        [Tooltip("스페이스바 입력에 맞춰 충격을 받을 RectTransform (비워두면 흔들림 비활성화). 미니게임 패널 루트를 지정하세요.")]
        [SerializeField] private RectTransform _shakeRoot;
        [Tooltip("한 번 누를 때 추가되는 충격 강도")]
        [SerializeField, Range(0f, 50f)] private float _shakeKick = 6f;
        [Tooltip("충격 강도 최댓값 (빠르게 연타해도 이 이상은 안 흔들림)")]
        [SerializeField, Range(0f, 100f)] private float _shakeMaxStrength = 25f;
        [Tooltip("충격 강도가 초당 감쇠되는 양 (클수록 빨리 잦아듦)")]
        [SerializeField, Range(1f, 80f)] private float _shakeDecayPerSecond = 35f;
        [Tooltip("한 번의 충격 지속 시간")]
        [SerializeField, Range(0.05f, 1f)] private float _shakeDuration = 0.25f;
        [Tooltip("Shake Vibrato (진동 횟수) — 클수록 파형이 조밀")]
        [SerializeField, Range(4, 60)] private int _shakeVibrato = 18;
        [Tooltip("Shake Randomness (방향 무작위성, 도) — 90이면 원형 충격 느낌")]
        [SerializeField, Range(0f, 180f)] private float _shakeRandomness = 90f;

        [Header("공통 결과 연출")]
        [SerializeField] private MiniGameResultEffect _resultEffect;

        private ButtonMashMiniGame _game;
        private float _hintTimer;
        private bool _hintShowPressed;

        // 흔들림 상태
        private Vector2 _shakeOriginalAnchoredPos;
        private bool _shakeOriginCaptured;
        private float _shakeStrength;
        private Tween _shakeTween;

        public void Initialize(IMiniGame game)
        {
            // 이전 게임 이벤트 구독 해제 (동일 뷰가 재사용될 때 누수 방지)
            if (_game != null)
            {
                _game.OnPressed -= HandlePressed;
            }

            _game = game as ButtonMashMiniGame;

            if (_game != null)
            {
                _game.OnPressed += HandlePressed;
            }

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

            // 흔들림 상태 초기화 및 위치 복구 (이전 미니게임에서 어긋난 상태로 시작하지 않도록)
            CaptureShakeOriginIfNeeded();
            ResetShake();
        }

        public void SetVisible(bool visible)
        {
            // 비활성화 직전에 위치를 원복시켜 다음 활성화 시 어긋나지 않도록 한다.
            if (!visible)
            {
                ResetShake();
            }

            gameObject.SetActive(visible);
        }

        public void UpdateView()
        {
            if (_game == null || _game.CurrentState != EMiniGameState.Playing)
            {
                // Playing 상태가 아니면 흔들림을 정리해 위치 어긋남을 방지한다.
                if (_shakeStrength > 0f || (_shakeTween != null && _shakeTween.IsActive()))
                {
                    ResetShake();
                }
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

            // 충격 강도 감쇠 (연타가 멈추면 점점 잦아듦)
            if (_shakeStrength > 0f)
            {
                _shakeStrength = Mathf.Max(0f, _shakeStrength - _shakeDecayPerSecond * Time.deltaTime);
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

            // 결과 연출이 시작되기 전 흔들림을 멈추고 위치를 원복한다.
            ResetShake();

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

        private void OnDestroy()
        {
            KillShakeTween();

            if (_game != null)
            {
                _game.OnPressed -= HandlePressed;
                _game = null;
            }
        }

        // 스페이스바가 눌릴 때마다 원형 충격 shake를 새로 실행한다.
        // 빠르게 연타하면 강도가 누적되어 더 격한 충격, 천천히 누르면 사이에 감쇠되어 약한 충격이 찍힌다.
        private void HandlePressed()
        {
            if (_shakeRoot == null)
            {
                return;
            }

            // 강도 누적 (최대치 캡)
            _shakeStrength = Mathf.Min(_shakeMaxStrength, _shakeStrength + _shakeKick);

            // 이전 shake tween을 정리하고 원위치에서 다시 시작 (어긋남 방지)
            KillShakeTween();

            // DOShakeAnchorPos: UGUI용 원형 충격 흔들림. randomness=90 → 모든 방향으로 튐.
            _shakeTween = _shakeRoot
                .DOShakeAnchorPos(
                    duration: _shakeDuration,
                    strength: _shakeStrength,
                    vibrato: _shakeVibrato,
                    randomness: _shakeRandomness,
                    snapping: false,
                    fadeOut: true)
                .SetUpdate(true)
                .OnKill(() => { if (_shakeOriginCaptured && _shakeRoot != null) _shakeRoot.anchoredPosition = _shakeOriginalAnchoredPos; })
                .OnComplete(() => { if (_shakeOriginCaptured && _shakeRoot != null) _shakeRoot.anchoredPosition = _shakeOriginalAnchoredPos; });
        }

        private void CaptureShakeOriginIfNeeded()
        {
            if (_shakeOriginCaptured || _shakeRoot == null)
            {
                return;
            }

            _shakeOriginalAnchoredPos = _shakeRoot.anchoredPosition;
            _shakeOriginCaptured = true;
        }

        private void KillShakeTween()
        {
            if (_shakeTween != null && _shakeTween.IsActive())
            {
                _shakeTween.Kill();
            }
            _shakeTween = null;

            if (_shakeRoot != null)
            {
                DOTween.Kill(_shakeRoot);
            }
        }

        private void ResetShake()
        {
            _shakeStrength = 0f;
            KillShakeTween();

            if (_shakeRoot != null && _shakeOriginCaptured)
            {
                _shakeRoot.anchoredPosition = _shakeOriginalAnchoredPos;
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
