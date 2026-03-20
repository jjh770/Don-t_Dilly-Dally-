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

        [Header("타이머")]
        [SerializeField] private Image _radialTimer;

        [Header("공통 결과 연출")]
        [SerializeField] private MiniGameResultEffect _resultEffect;

        private DirectionQTEMiniGame _game;
        private readonly List<QTEArrowSlot> _slots = new List<QTEArrowSlot>();
        private int _lastPromptIndex = -1;
        private bool _slotsBuilt;
        private Tween _slideTween;

        public void Initialize(IMiniGame game)
        {
            _game = game as DirectionQTEMiniGame;
            _lastPromptIndex = -1;
            _slotsBuilt = false;

            if (_resultEffect != null)
            {
                _resultEffect.Reset();
            }

            TryBuildSlots();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);

            if (!visible)
            {
                ClearSlots();
                _slotsBuilt = false;
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

        /// <summary>
        /// 컨테이너의 X 위치를 이동하여 targetIndex 슬롯이 중앙에 오게 한다.
        /// </summary>
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

        /// <summary>
        /// 클리어된 슬롯의 알파를 거리에 따라 페이드 처리한다.
        /// distance 1 → 알파 0.75, distance 2 → 0.5, distance 3 → 0.25, distance 4+ → 0
        /// </summary>
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

        private void UpdateRadialTimer()
        {
            if (_radialTimer == null) return;
            _radialTimer.fillAmount = _game.RemainingTimeRatio;
        }

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
            ClearSlots();
        }
    }
}
