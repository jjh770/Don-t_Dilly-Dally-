using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class DirectionQTEUIView : MonoBehaviour, IMiniGameUIView
    {
        [Header("화살표 이미지")]
        [SerializeField] private Sprite _upSprite;
        [SerializeField] private Sprite _downSprite;
        [SerializeField] private Sprite _leftSprite;
        [SerializeField] private Sprite _rightSprite;

        [Header("슬롯")]
        [SerializeField] private QTEArrowSlot _slotPrefab;
        [SerializeField] private RectTransform _slotContainer;

        [Header("슬라이드 설정")]
        [Tooltip("슬롯 간 간격 (px)")]
        [SerializeField] private float _slotSpacing = 120f;
        [Tooltip("슬라이드 애니메이션 시간")]
        [SerializeField] private float _slideDuration = 0.25f;
        [Tooltip("클리어된 슬롯이 완전히 사라지기까지의 거리 (칸 수)")]
        [SerializeField] private int _fadeOutSlotCount = 4;
        [Tooltip("알파 페이드 시간")]
        [SerializeField] private float _fadeDuration = 0.2f;

        [Header("빛나는 테두리 (현재 방향키 강조)")]
        [Tooltip("2번째 테두리 이미지 (알파 펄스)")]
        [SerializeField] private Image _glowBorder2;
        [Tooltip("3번째 테두리 이미지 (알파 펄스)")]
        [SerializeField] private Image _glowBorder3;
        [Tooltip("테두리 알파 펄스 한 사이클 시간 (초)")]
        [SerializeField] private float _glowPulseDuration = 0.6f;

        [Header("타이머")]
        [SerializeField] private Image _radialTimer;

        [Header("타이머 그라디언트 색상")]
        [SerializeField] private Color _timerColorFull = new Color(0.4f, 1f, 0.2f);
        [SerializeField] private Color _timerColorMid = new Color(1f, 0.6f, 0f);
        [SerializeField] private Color _timerColorEmpty = new Color(1f, 0.2f, 0.2f);

        [Header("공통 결과 연출")]
        [SerializeField] private MiniGameResultEffect _resultEffect;

        private DirectionQTEMiniGame _game;
        private readonly List<QTEArrowSlot> _slots = new List<QTEArrowSlot>();
        private int _lastPromptIndex = -1;
        private bool _slotsBuilt;
        private Tween _slideTween;
        private Sequence _glowSequence;

        public void Initialize(IMiniGame game)
        {
            _game = game as DirectionQTEMiniGame;
            _lastPromptIndex = -1;
            _slotsBuilt = false;

            if (_radialTimer != null)
            {
                _radialTimer.fillAmount = 1f;
                _radialTimer.color = _timerColorFull;
            }

            if (_resultEffect != null)
            {
                _resultEffect.Reset();
            }

            TryBuildSlots();
            StartGlowPulse();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);

            if (!visible)
            {
                ClearSlots();
                _slotsBuilt = false;
                StopGlowPulse();
            }
        }

        public void UpdateView()
        {
            if (_game == null || _game.CurrentState != EMiniGameState.Playing) return;

            if (!_slotsBuilt)
            {
                TryBuildSlots();
            }

            UpdateRadialTimer();
            UpdateSlotStates();
        }

        public void ShowResult(bool isSuccess)
        {
            StopGlowPulse();

            if (_resultEffect != null)
            {
                if (isSuccess)
                    _resultEffect.PlaySuccess();
                else
                    _resultEffect.PlayFail();
            }
        }

        private void TryBuildSlots()
        {
            if (_slotsBuilt) return;
            if (_game == null || _slotPrefab == null || _slotContainer == null) return;

            var prompts = _game.Prompts;
            if (prompts == null) return;

            ClearSlots();

            for (int i = 0; i < prompts.Length; i++)
            {
                QTEArrowSlot slot = Instantiate(_slotPrefab, _slotContainer);
                slot.SetSprite(DirectionToSprite(prompts[i].EQteDirection));
                slot.SetState(ESlotState.Pending);

                // 슬롯을 수평으로 배치 (0번 슬롯이 x=0).
                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(i * _slotSpacing, 0f);

                _slots.Add(slot);
            }

            // 첫 번째 슬롯을 현재 상태로 설정.
            if (_slots.Count > 0)
            {
                _slots[0].SetState(ESlotState.Current);
            }

            // 컨테이너 위치를 0번 슬롯이 중앙에 오도록 설정.
            _slotContainer.anchoredPosition = new Vector2(0f, _slotContainer.anchoredPosition.y);

            _slotsBuilt = true;
        }

        private void ClearSlots()
        {
            KillSlideTween();

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].ResetSlot();
                    Destroy(_slots[i].gameObject);
                }
            }
            _slots.Clear();
        }

        private void UpdateSlotStates()
        {
            int currentIndex = _game.CurrentPromptIndex;
            if (currentIndex == _lastPromptIndex) return;

            // 이전 슬롯 → Cleared 처리.
            if (_lastPromptIndex >= 0 && _lastPromptIndex < _slots.Count)
            {
                bool wasSuccess = _game.LastInputResult == true;
                _slots[_lastPromptIndex].SetState(wasSuccess ? ESlotState.Cleared : ESlotState.Failed);
            }

            // 현재 슬롯 → Current 처리.
            if (currentIndex < _slots.Count)
            {
                _slots[currentIndex].SetState(ESlotState.Current);
            }

            // 컨테이너를 슬라이드하여 현재 슬롯이 중앙에 오도록.
            SlideToIndex(currentIndex);

            // 클리어된 슬롯들의 알파값 갱신.
            UpdateClearedAlpha(currentIndex);

            _lastPromptIndex = currentIndex;
        }

        private void SlideToIndex(int targetIndex)
        {
            KillSlideTween();

            float targetX = -targetIndex * _slotSpacing;
            float currentY = _slotContainer.anchoredPosition.y;

            _slideTween = _slotContainer
                .DOAnchorPos(new Vector2(targetX, currentY), _slideDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void UpdateClearedAlpha(int currentIndex)
        {
            for (int i = 0; i < currentIndex && i < _slots.Count; i++)
            {
                int distance = currentIndex - i;

                float targetAlpha;
                if (distance >= _fadeOutSlotCount)
                {
                    targetAlpha = 0f;
                }
                else
                {
                    targetAlpha = 1f - (float)distance / _fadeOutSlotCount;
                }

                _slots[i].FadeAlpha(targetAlpha, _fadeDuration);
            }
        }

        // ── 빛나는 테두리 순차 펄스 ──
        // 2번 0→1 → 3번 0→1 → 둘 다 0으로 리셋 → 무한 반복

        private void StartGlowPulse()
        {
            StopGlowPulse();
            if (_glowBorder2 == null && _glowBorder3 == null) return;

            SetImageAlpha(_glowBorder2, 0f);
            SetImageAlpha(_glowBorder3, 0f);

            _glowSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLoops(-1, LoopType.Restart);

            // 2번 테두리: 0→1
            if (_glowBorder2 != null)
            {
                _glowSequence.Append(
                    DOTween.ToAlpha(() => _glowBorder2.color, c => _glowBorder2.color = c,
                        1f, _glowPulseDuration).SetEase(Ease.InOutSine));
            }

            // 3번 테두리: 0→1 (2번 완료 후 시작)
            if (_glowBorder3 != null)
            {
                _glowSequence.Append(
                    DOTween.ToAlpha(() => _glowBorder3.color, c => _glowBorder3.color = c,
                        1f, _glowPulseDuration).SetEase(Ease.InOutSine));
            }

            // 둘 다 동시에 0으로 페이드아웃
            if (_glowBorder2 != null)
            {
                _glowSequence.Append(
                    DOTween.ToAlpha(() => _glowBorder2.color, c => _glowBorder2.color = c,
                        0f, _glowPulseDuration * 0.5f).SetEase(Ease.InOutSine));
            }
            if (_glowBorder3 != null)
            {
                // Join → 2번과 동시에 페이드아웃
                _glowSequence.Join(
                    DOTween.ToAlpha(() => _glowBorder3.color, c => _glowBorder3.color = c,
                        0f, _glowPulseDuration * 0.5f).SetEase(Ease.InOutSine));
            }
        }

        private void StopGlowPulse()
        {
            if (_glowSequence != null && _glowSequence.IsActive())
            {
                _glowSequence.Kill();
                _glowSequence = null;
            }

            SetImageAlpha(_glowBorder2, 0f);
            SetImageAlpha(_glowBorder3, 0f);
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null) return;
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        // ── 타이머 ──

        private void UpdateRadialTimer()
        {
            if (_radialTimer == null) return;

            float timeRatio = _game.RemainingTimeRatio;
            _radialTimer.fillAmount = timeRatio;
            _radialTimer.color = EvaluateTimerColor(timeRatio);
        }

        private Color EvaluateTimerColor(float t)
        {
            if (t >= 0.5f)
                return Color.Lerp(_timerColorMid, _timerColorFull, (t - 0.5f) * 2f);
            else
                return Color.Lerp(_timerColorEmpty, _timerColorMid, t * 2f);
        }

        // ── 유틸 ──

        private void KillSlideTween()
        {
            if (_slideTween != null && _slideTween.IsActive())
            {
                _slideTween.Kill();
                _slideTween = null;
            }
        }

        private Sprite DirectionToSprite(EQteDirection dir)
        {
            return dir switch
            {
                EQteDirection.Up => _upSprite,
                EQteDirection.Down => _downSprite,
                EQteDirection.Left => _leftSprite,
                EQteDirection.Right => _rightSprite,
                _ => null
            };
        }

        private void OnDestroy()
        {
            StopGlowPulse();
            ClearSlots();
        }
    }
}
