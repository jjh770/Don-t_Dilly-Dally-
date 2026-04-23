using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class MiniGameUIController : MonoBehaviour
    {
        [SerializeField] private ButtonMashUIView _buttonMashView;
        [SerializeField] private DirectionQTEUIView _directionQTEView;
        [SerializeField] private PrecisionStopUIView _precisionStopView;
        [SerializeField] private ReCaptchaUIView _reCaptchaView;

        [Header("공통 UI")]
        [SerializeField] private GameObject _overlayPanel;

        [Header("등장 연출")]
        [Tooltip("활성 미니게임 View가 스케일 0에서 원본 크기로 튀어나오는 시간(초)")]
        [SerializeField, Range(0.1f, 1f)] private float _entranceDuration = 0.35f;

        private IMiniGameUIView _activeView;

        // 각 View root의 원본 localScale을 첫 표시 시점에 캐시. 이후 복원 기준값으로 사용.
        private readonly Dictionary<Transform, Vector3> _viewOriginalScales = new();
        private Transform _activeViewTransform;
        private Tween _entranceTween;

        public void ShowMiniGameUI(IMiniGame game)
        {
            _overlayPanel.SetActive(true);

            _activeView = game.GameType switch
            {
                MiniGameType.ButtonMash => _buttonMashView,
                MiniGameType.DirectionQTE => _directionQTEView,
                MiniGameType.PrecisionStop => _precisionStopView,
                MiniGameType.ReCaptcha => _reCaptchaView,
                _ => null
            };

            _activeView?.Initialize(game);
            _activeView?.SetVisible(true);

            PlayEntranceAnimationOnActiveView();
        }

        public void ShowResult(bool isSuccess)
        {
            _activeView?.ShowResult(isSuccess);
        }

        public void HideMiniGameUI()
        {
            // 등장 연출을 중단하고 원본 스케일로 복원해 다음 표시 시 0→원본 트윈이 깨끗하게 재시작되도록.
            KillEntranceTween();
            RestoreActiveViewScale();

            _activeView?.SetVisible(false);
            _activeView = null;
            _activeViewTransform = null;
            _overlayPanel.SetActive(false);
        }

        private void Update()
        {
            _activeView?.UpdateView();
        }

        private void OnDestroy()
        {
            KillEntranceTween();
        }

        // Canvas(SS-Overlay)의 localScale은 Unity가 매 프레임 덮어쓰므로 무시된다.
        // 반면 Canvas 하위 RectTransform은 정상 적용되므로 활성 View root를 대상으로 트윈한다.
        private void PlayEntranceAnimationOnActiveView()
        {
            if (_activeView is not Component activeComponent)
            {
                return;
            }

            Transform target = activeComponent.transform;

            if (!_viewOriginalScales.TryGetValue(target, out Vector3 originalScale))
            {
                originalScale = target.localScale;
                _viewOriginalScales[target] = originalScale;
            }

            KillEntranceTween();

            target.localScale = Vector3.zero;
            _activeViewTransform = target;
            _entranceTween = target
                .DOScale(originalScale, _entranceDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private void RestoreActiveViewScale()
        {
            if (_activeViewTransform == null)
            {
                return;
            }

            if (_viewOriginalScales.TryGetValue(_activeViewTransform, out Vector3 originalScale))
            {
                _activeViewTransform.localScale = originalScale;
            }
        }

        private void KillEntranceTween()
        {
            if (_entranceTween != null && _entranceTween.IsActive())
            {
                _entranceTween.Kill();
            }

            _entranceTween = null;
        }
    }
}
