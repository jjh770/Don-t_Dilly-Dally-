using UnityEngine;

namespace DontDillyDally.MiniGame
{
    // 체크박스 클릭 한 번이 유일한 입력.
    // 클릭 후 짧은 로딩 스피너를 돌린 뒤 성공 처리하며, 제한 시간 내에 클릭이 없으면 실패한다.
    public sealed class ReCaptchaMiniGame : IMiniGame
    {
        public MiniGameType GameType => MiniGameType.ReCaptcha;

        public EMiniGameState CurrentState { get; private set; } = EMiniGameState.Idle;

        // 로딩 바 게이지로 활용 (클릭 전 0, 로딩 중 0→1, 성공 연출 대기 중에는 1).
        public float NormalizedProgress => _isHoldingSuccess ? 1f : _loadingProgress;

        public event System.Action<MiniGameResult> OnCompleted;

        // UI가 "로딩 중" 여부를 읽어 스피너/체크박스 전환에 사용한다.
        public bool IsLoading => _isLoading;

        // 로딩 끝난 후 체크표시 연출을 보여주는 대기 페이즈. UI가 이걸 감지해 체크마크 fill을 시작한다.
        public bool IsHoldingSuccess => _isHoldingSuccess;

        public float RemainingTimeRatio => _config != null && _config.TimeLimit > 0f
            ? Mathf.Clamp01(1f - _elapsedTime / _config.TimeLimit)
            : 0f;

        private ReCaptchaConfig _config;
        private float _elapsedTime;
        private float _loadingTimer;
        private float _loadingProgress;
        private bool _isLoading;
        private bool _isHoldingSuccess;
        private float _successHoldTimer;

        public void Begin(MiniGameConfig config)
        {
            _config = config as ReCaptchaConfig;
            if (_config == null)
            {
                Debug.LogError("[ReCaptcha] 잘못된 Config 타입이 전달됨");
                return;
            }

            _elapsedTime = 0f;
            _loadingTimer = 0f;
            _loadingProgress = 0f;
            _isLoading = false;
            _isHoldingSuccess = false;
            _successHoldTimer = 0f;
            CurrentState = EMiniGameState.Playing;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != EMiniGameState.Playing)
            {
                return;
            }

            // 성공 연출 대기 페이즈: 이미 승리 확정이므로 타이머는 정지. 체크표시가 충분히 보인 뒤 Succeeded 판정.
            if (_isHoldingSuccess)
            {
                _successHoldTimer += deltaTime;
                if (_successHoldTimer >= _config.SuccessHoldDuration)
                {
                    CurrentState = EMiniGameState.Succeeded;
                    OnCompleted?.Invoke(new MiniGameResult(GameType, true, _elapsedTime));
                }
                return;
            }

            _elapsedTime += deltaTime;

            // 로딩 중이라도 타이머는 계속 흐른다. 타임아웃이 먼저 터지면 실패 처리.
            if (_elapsedTime >= _config.TimeLimit)
            {
                CurrentState = EMiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
                return;
            }

            if (_isLoading)
            {
                _loadingTimer += deltaTime;
                _loadingProgress = Mathf.Clamp01(_loadingTimer / _config.LoadingDuration);

                // 로딩 완료: 바로 Succeeded로 가지 않고, 체크표시 연출을 위한 Hold 페이즈로 전환.
                if (_loadingTimer >= _config.LoadingDuration)
                {
                    _isLoading = false;
                    _isHoldingSuccess = true;
                    _successHoldTimer = 0f;
                }
            }
        }

        // UI 체크박스 버튼이 클릭됐을 때 호출. 이미 로딩 중이면 무시한다.
        public void NotifyCheckboxClicked()
        {
            if (CurrentState != EMiniGameState.Playing || _isLoading)
            {
                return;
            }

            _isLoading = true;
            _loadingTimer = 0f;
        }

        public void Abort()
        {
            if (CurrentState != EMiniGameState.Playing)
            {
                return;
            }

            CurrentState = EMiniGameState.Failed;
            OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
        }
    }
}
