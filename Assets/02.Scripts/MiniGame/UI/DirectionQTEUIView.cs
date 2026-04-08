using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class DirectionQTEUIView : MiniGameUIViewBase<DirectionQTEMiniGame>
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
        [Tooltip("정답 펀치가 먼저 보이도록 슬라이드를 늦추는 시간 (초)")]
        [SerializeField, Range(0f, 0.5f)] private float _slideDelay = 0.12f;

        [Header("빛나는 테두리 (현재 방향키 강조)")]
        [Tooltip("테두리 최상위 오브젝트 (정답 시 띠용 효과)")]
        [SerializeField] private RectTransform _glowBorderRoot;
        [Tooltip("2번째 테두리 이미지 (알파 펄스)")]
        [SerializeField] private Image _glowBorder2;
        [Tooltip("3번째 테두리 이미지 (알파 펄스)")]
        [SerializeField] private Image _glowBorder3;
        [Tooltip("테두리 알파 펄스 한 사이클 시간 (초)")]
        [SerializeField] private float _glowPulseDuration = 0.6f;
        [Tooltip("정답 시 띠용 크기")]
        [SerializeField] private float _glowPunchScale = 0.25f;
        [Tooltip("정답 시 띠용 시간")]
        [SerializeField] private float _glowPunchDuration = 0.25f;

        [Header("타이머")]
        [SerializeField] private RadialTimerView _timer;

        [Header("오답 흔들림 연출")]
        [Tooltip("오답 시 흔들릴 전체 UI RectTransform (비워두면 흔들림 비활성화). QTE 패널 루트를 지정하세요.")]
        [SerializeField] private RectTransform _shakeRoot;
        [Tooltip("흔들림 강도 (px)")]
        [SerializeField, Range(0f, 100f)] private float _failShakeStrength = 25f;
        [Tooltip("흔들림 지속 시간")]
        [SerializeField, Range(0.1f, 2f)] private float _failShakeDuration = 0.5f;
        [Tooltip("Shake Vibrato (진동 횟수)")]
        [SerializeField, Range(4, 60)] private int _failShakeVibrato = 20;
        [Tooltip("Shake Randomness (방향 무작위성, 도) — 90이면 원형")]
        [SerializeField, Range(0f, 180f)] private float _failShakeRandomness = 90f;

        private readonly List<QTEArrowSlot> _slots = new List<QTEArrowSlot>();
        private int _lastPromptIndex = -1;
        private bool _slotsBuilt;
        private Tween _slideTween;
        private Sequence _glowSequence;
        private Tween _failShakeTween;
        private Vector2 _shakeRootOriginalPos;
        private bool _shakeRootOriginCaptured;

        protected override void OnInitialize()
        {
            _lastPromptIndex = -1;
            _slotsBuilt = false;

            _timer.Initialize();

            TryBuildSlots();
            StartGlowPulse();

            // 흔들림 원위치 캡처 및 복구 (이전 실패로 어긋난 상태로 시작하지 않도록)
            CaptureShakeOriginIfNeeded();
            ResetFailShake();
        }

        public override void SetVisible(bool visible)
        {
            // 비활성화 직전에 흔들림을 정리해 다음 활성화 시 어긋나지 않도록 한다.
            if (!visible)
            {
                ResetFailShake();
            }

            gameObject.SetActive(visible);

            if (!visible)
            {
                ClearSlots();
                _slotsBuilt = false;
                StopGlowPulse();
            }
        }

        public override void UpdateView()
        {
            if (Game == null || Game.CurrentState != EMiniGameState.Playing)
            {
                return;
            }

            if (!_slotsBuilt)
            {
                TryBuildSlots();
            }

            _timer.SetRatio(Game.RemainingTimeRatio);
            UpdateSlotStates();
        }

        protected override void OnBeforeShowResult(bool isSuccess)
        {
            // 글로우 펄스 루프를 먼저 정리 (Kill이 뒤에서 찍을 펀치를 죽이지 않도록)
            StopGlowPulse();

            // 마지막 정답 슬롯이 아직 Cleared 처리되지 못했으면 지금 펀치를 찍어준다.
            // (마지막 정답 입력 → 즉시 Succeeded 상태 전환 → UpdateView의 early return으로
            //  UpdateSlotStates가 호출되지 않아 마지막 슬롯 펀치가 누락되는 케이스)
            // _lastPromptIndex는 Current 전환 시점에 이미 증가해 있어서 인덱스로는 판단 불가능 →
            // 슬롯의 실제 state를 직접 확인한다.
            if (isSuccess && _slots.Count > 0)
            {
                int finalIdx = _slots.Count - 1;
                if (_slots[finalIdx].State != ESlotState.Cleared)
                {
                    _slots[finalIdx].SetState(ESlotState.Cleared);
                    PlayGlowPunch();
                    _lastPromptIndex = finalIdx;
                }
            }

            // 실패 시 전체 UI 흔들림 (ButtonMash Fail 스타일)
            if (!isSuccess)
            {
                PlayFailShake();
            }
        }

        private void TryBuildSlots()
        {
            if (_slotsBuilt)
            {
                return;
            }

            if (Game == null || _slotPrefab == null || _slotContainer == null)
            {
                return;
            }

            var prompts = Game.Prompts;
            if (prompts == null)
            {
                return;
            }

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
            int currentIndex = Game.CurrentPromptIndex;
            if (currentIndex == _lastPromptIndex)
            {
                return;
            }

            // 이전 슬롯 → Cleared 처리.
            if (_lastPromptIndex >= 0 && _lastPromptIndex < _slots.Count)
            {
                bool wasSuccess = Game.LastInputResult == true;
                _slots[_lastPromptIndex].SetState(wasSuccess ? ESlotState.Cleared : ESlotState.Failed);

                // 정답 시 글로우 테두리 띠용 (전체 UI 펀치)
                if (wasSuccess)
                {
                    PlayGlowPunch();
                }
            }

            // 현재 슬롯 → Current 처리.
            if (currentIndex < _slots.Count)
            {
                _slots[currentIndex].SetState(ESlotState.Current);
            }

            // 컨테이너를 슬라이드하여 현재 슬롯이 중앙에 오도록.
            SlideToIndex(currentIndex);

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
                .SetDelay(_slideDelay)
                .SetUpdate(true);
        }

        // ── 빛나는 테두리 순차 펄스 ──
        // 2번 0→1 → 3번 0→1 → 둘 다 0으로 리셋 → 무한 반복

        private void StartGlowPulse()
        {
            StopGlowPulse();
            if (_glowBorder2 == null && _glowBorder3 == null)
            {
                return;
            }

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

            if (_glowBorderRoot != null)
            {
                DOTween.Kill(_glowBorderRoot);
                _glowBorderRoot.localScale = Vector3.one;
            }

            SetImageAlpha(_glowBorder2, 0f);
            SetImageAlpha(_glowBorder3, 0f);
        }

        private void PlayGlowPunch()
        {
            if (_glowBorderRoot == null)
            {
                return;
            }

            DOTween.Kill(_glowBorderRoot);
            _glowBorderRoot.localScale = Vector3.one;
            _glowBorderRoot
                .DOPunchScale(Vector3.one * _glowPunchScale, _glowPunchDuration, 8, 0.5f)
                .SetUpdate(true);
        }

        // ── 오답 시 전체 UI 흔들림 ──

        private void CaptureShakeOriginIfNeeded()
        {
            if (_shakeRootOriginCaptured || _shakeRoot == null)
            {
                return;
            }

            _shakeRootOriginalPos = _shakeRoot.anchoredPosition;
            _shakeRootOriginCaptured = true;
        }

        private void PlayFailShake()
        {
            if (_shakeRoot == null)
            {
                return;
            }

            CaptureShakeOriginIfNeeded();

            // 이전 shake tween 정리 및 원위치 복구
            if (_failShakeTween != null && _failShakeTween.IsActive())
            {
                _failShakeTween.Kill();
            }
            DOTween.Kill(_shakeRoot);
            _shakeRoot.anchoredPosition = _shakeRootOriginalPos;

            _failShakeTween = _shakeRoot
                .DOShakeAnchorPos(
                    duration: _failShakeDuration,
                    strength: _failShakeStrength,
                    vibrato: _failShakeVibrato,
                    randomness: _failShakeRandomness,
                    snapping: false,
                    fadeOut: true)
                .SetUpdate(true)
                .OnKill(() => { if (_shakeRootOriginCaptured && _shakeRoot != null) _shakeRoot.anchoredPosition = _shakeRootOriginalPos; })
                .OnComplete(() => { if (_shakeRootOriginCaptured && _shakeRoot != null) _shakeRoot.anchoredPosition = _shakeRootOriginalPos; });
        }

        private void ResetFailShake()
        {
            if (_failShakeTween != null && _failShakeTween.IsActive())
            {
                _failShakeTween.Kill();
                _failShakeTween = null;
            }

            if (_shakeRoot != null)
            {
                DOTween.Kill(_shakeRoot);
                if (_shakeRootOriginCaptured)
                {
                    _shakeRoot.anchoredPosition = _shakeRootOriginalPos;
                }
            }
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Color c = image.color;
            c.a = alpha;
            image.color = c;
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
            ResetFailShake();
        }
    }
}
