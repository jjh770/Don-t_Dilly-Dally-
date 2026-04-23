using DG.Tweening;
using DontDillyDally.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.MiniGame
{
    public sealed class ReCaptchaUIView : MiniGameUIViewBase<ReCaptchaMiniGame>
    {
        [Header("체크박스 입력")]
        [Tooltip("플레이어가 클릭할 체크박스 버튼")]
        [SerializeField] private Button _checkboxButton;

        [Header("상태별 표시 오브젝트")]
        [Tooltip("클릭 전: 빈 체크박스")]
        [SerializeField] private GameObject _emptyCheckbox;

        [Tooltip("클릭 후 로딩 중: 회전하는 스피너")]
        [SerializeField] private GameObject _loadingSpinner;

        [Tooltip("스피너를 투명하게 페이드아웃하기 위한 CanvasGroup (스피너 오브젝트에 추가)")]
        [SerializeField] private CanvasGroup _loadingSpinnerCanvasGroup;

        [Tooltip("성공 직후: 체크 표시 컨테이너")]
        [SerializeField] private GameObject _checkMark;

        [Tooltip("체크 표시가 그려지는 연출용 Image. Image Type=Filled, Fill Method=Horizontal, Origin=Left 권장")]
        [SerializeField] private Image _checkMarkFill;

        [Header("스피너 회전")]
        [Tooltip("초당 스피너 회전 속도(도)")]
        [SerializeField] private float _spinnerSpeed = 360f;

        [Header("전환 연출")]
        [Tooltip("체크박스 → 스피너 스케일 전환 각각의 시간(초)")]
        [SerializeField, Range(0.05f, 1f)] private float _checkboxToSpinnerDuration = 0.3f;

        [Tooltip("체크박스가 줄어들기 시작한 후 스피너가 등장할 때까지의 지연(초). 값을 키울수록 체크박스와 스피너가 명확히 분리되어 보인다.")]
        [SerializeField, Range(0f, 0.8f)] private float _spinnerEnterDelay = 0.15f;

        [Tooltip("성공 시 스피너가 투명하게 사라지는 시간(초)")]
        [SerializeField, Range(0.05f, 1f)] private float _spinnerFadeOutDuration = 0.25f;

        [Tooltip("체크 표시가 왼쪽에서 오른쪽으로 차오르는 시간(초)")]
        [SerializeField, Range(0.05f, 1f)] private float _checkMarkDrawDuration = 0.3f;

        [Header("남은 시간 타이머 (선택)")]
        [SerializeField] private RadialTimerView _timer;

        // 프리팹의 원본 스케일을 캐시. 0으로 줄였다가 복원할 때 사용한다.
        private RectTransform _emptyCheckboxRect;
        private RectTransform _loadingSpinnerRect;
        private Vector3 _emptyCheckboxOriginalScale = Vector3.one;
        private Vector3 _loadingSpinnerOriginalScale = Vector3.one;
        private bool _scalesCaptured;

        private Tween _checkboxTween;
        private Tween _spinnerTween;
        private Tween _spinnerFadeTween;
        private Tween _checkMarkFillTween;

        // 체크표시 연출을 한 번만 시작하도록 표시. Game의 IsHoldingSuccess가 true로 전환되는 순간 감지용.
        private bool _hasStartedCheckMarkReveal;

        protected override void OnInitialize()
        {
            CaptureOriginalScalesIfNeeded();

            if (_checkboxButton != null)
            {
                // Initialize가 재호출되는 경로 대비. 리스너는 한 번씩만 유지.
                _checkboxButton.onClick.RemoveListener(HandleCheckboxClicked);
                _checkboxButton.onClick.AddListener(HandleCheckboxClicked);
                _checkboxButton.interactable = true;
            }

            KillAllTweens();
            ResetVisualsToInitial();
            _hasStartedCheckMarkReveal = false;

            if (_timer != null)
            {
                _timer.Initialize();
            }
        }

        public override void SetVisible(bool visible)
        {
            // 비활성화 직전에 트윈을 정리하고 시각 상태를 초기화해 다음 활성화 시 깨끗한 상태로 시작.
            if (!visible)
            {
                KillAllTweens();
                ResetVisualsToInitial();
            }

            gameObject.SetActive(visible);
        }

        public override void UpdateView()
        {
            if (Game == null)
            {
                return;
            }

            if (Game.CurrentState != EMiniGameState.Playing)
            {
                return;
            }

            // Hold 페이즈 진입 순간을 감지해 체크표시 연출을 한 번 시작.
            // 이 시점부터 게임은 아직 Playing이지만 사실상 승리 확정이라 타이머도 정지시켜 보여준다.
            if (Game.IsHoldingSuccess)
            {
                if (!_hasStartedCheckMarkReveal)
                {
                    _hasStartedCheckMarkReveal = true;
                    PlayCheckMarkFill();
                }
                return;
            }

            if (Game.IsLoading)
            {
                RotateSpinner();
            }

            // 로딩 중에도 타이머는 계속 흐름 (Game.Tick이 elapsedTime 계속 증가시킴).
            if (_timer != null)
            {
                _timer.SetRatio(Game.RemainingTimeRatio);
            }
        }

        protected override void OnBeforeShowResult(bool isSuccess)
        {
            if (_checkboxButton != null)
            {
                _checkboxButton.interactable = false;
            }

            // 정상 경로에서는 Hold 페이즈에서 이미 PlayCheckMarkFill이 시작됐다.
            // 만약 어떤 이유로 Hold 없이 바로 Succeeded가 됐다면 여기서 즉시 그려서 결과가 비어 보이지 않도록.
            if (isSuccess)
            {
                if (!_hasStartedCheckMarkReveal)
                {
                    _hasStartedCheckMarkReveal = true;
                    PlayCheckMarkFill();
                }
            }
            else
            {
                PlayLocalSfx(SFXKey.MiniGameFail);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            KillAllTweens();
        }

        private void OnDestroy()
        {
            KillAllTweens();

            if (_checkboxButton != null)
            {
                _checkboxButton.onClick.RemoveListener(HandleCheckboxClicked);
            }
        }

        private void HandleCheckboxClicked()
        {
            if (Game == null)
            {
                return;
            }

            if (Game.CurrentState != EMiniGameState.Playing || Game.IsLoading)
            {
                return;
            }

            Game.NotifyCheckboxClicked();
            PlayCheckboxToSpinnerTransition();
        }

        // 체크박스를 스케일 0으로 줄이며 사라지게 하고, 스피너를 0에서 원본 스케일로 키워 등장시킨다.
        private void PlayCheckboxToSpinnerTransition()
        {
            if (_checkboxButton != null)
            {
                _checkboxButton.interactable = false;
            }

            if (_emptyCheckboxRect != null)
            {
                KillTween(ref _checkboxTween);
                _checkboxTween = _emptyCheckboxRect
                    .DOScale(Vector3.zero, _checkboxToSpinnerDuration)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (_emptyCheckbox != null)
                        {
                            _emptyCheckbox.SetActive(false);
                        }
                    });
            }
            else if (_emptyCheckbox != null)
            {
                _emptyCheckbox.SetActive(false);
            }

            if (_loadingSpinner != null)
            {
                _loadingSpinner.SetActive(true);
            }

            // 스피너 알파를 미리 1로 보정 (이전 플레이의 페이드아웃 값이 남아있을 수 있음).
            if (_loadingSpinnerCanvasGroup != null)
            {
                _loadingSpinnerCanvasGroup.alpha = 1f;
            }

            if (_loadingSpinnerRect != null)
            {
                KillTween(ref _spinnerTween);
                _loadingSpinnerRect.localScale = Vector3.zero;
                _spinnerTween = _loadingSpinnerRect
                    .DOScale(_loadingSpinnerOriginalScale, _checkboxToSpinnerDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(_spinnerEnterDelay)
                    .SetUpdate(true);
            }
        }

        // 체크 표시를 fillAmount 0→1로 그려지듯 표시. 스피너는 알파로 페이드아웃한 뒤 비활성화.
        private void PlayCheckMarkFill()
        {
            FadeOutSpinner();

            if (_checkMark != null)
            {
                _checkMark.SetActive(true);
            }

            if (_checkMarkFill == null)
            {
                return;
            }

            _checkMarkFill.fillAmount = 0f;
            KillTween(ref _checkMarkFillTween);
            _checkMarkFillTween = DOTween
                .To(() => _checkMarkFill.fillAmount, v => _checkMarkFill.fillAmount = v, 1f, _checkMarkDrawDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        // 스피너를 알파 1→0 페이드 후 비활성화. CanvasGroup이 없으면 즉시 비활성화.
        private void FadeOutSpinner()
        {
            if (_loadingSpinner == null)
            {
                return;
            }

            if (_loadingSpinnerCanvasGroup == null)
            {
                _loadingSpinner.SetActive(false);
                return;
            }

            KillTween(ref _spinnerFadeTween);
            _spinnerFadeTween = _loadingSpinnerCanvasGroup
                .DOFade(0f, _spinnerFadeOutDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (_loadingSpinner != null)
                    {
                        _loadingSpinner.SetActive(false);
                    }
                });
        }

        private void ResetVisualsToInitial()
        {
            if (_emptyCheckbox != null)
            {
                _emptyCheckbox.SetActive(true);
            }

            if (_emptyCheckboxRect != null && _scalesCaptured)
            {
                _emptyCheckboxRect.localScale = _emptyCheckboxOriginalScale;
            }

            if (_loadingSpinner != null)
            {
                _loadingSpinner.SetActive(false);
            }

            if (_loadingSpinnerRect != null && _scalesCaptured)
            {
                _loadingSpinnerRect.localScale = _loadingSpinnerOriginalScale;
            }

            // 다음 플레이를 위해 알파 복원.
            if (_loadingSpinnerCanvasGroup != null)
            {
                _loadingSpinnerCanvasGroup.alpha = 1f;
            }

            if (_checkMark != null)
            {
                _checkMark.SetActive(false);
            }

            if (_checkMarkFill != null)
            {
                _checkMarkFill.fillAmount = 0f;
            }

            _hasStartedCheckMarkReveal = false;
        }

        private void RotateSpinner()
        {
            if (_loadingSpinner == null)
            {
                return;
            }

            _loadingSpinner.transform.Rotate(0f, 0f, -_spinnerSpeed * Time.deltaTime);
        }

        private void CaptureOriginalScalesIfNeeded()
        {
            if (_scalesCaptured)
            {
                return;
            }

            if (_emptyCheckbox != null)
            {
                _emptyCheckboxRect = _emptyCheckbox.transform as RectTransform;
                if (_emptyCheckboxRect != null)
                {
                    _emptyCheckboxOriginalScale = _emptyCheckboxRect.localScale;
                }
            }

            if (_loadingSpinner != null)
            {
                _loadingSpinnerRect = _loadingSpinner.transform as RectTransform;
                if (_loadingSpinnerRect != null)
                {
                    _loadingSpinnerOriginalScale = _loadingSpinnerRect.localScale;
                }
            }

            _scalesCaptured = true;
        }

        private void KillAllTweens()
        {
            KillTween(ref _checkboxTween);
            KillTween(ref _spinnerTween);
            KillTween(ref _spinnerFadeTween);
            KillTween(ref _checkMarkFillTween);
        }

        private void KillTween(ref Tween tween)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }

            tween = null;
        }
    }
}
