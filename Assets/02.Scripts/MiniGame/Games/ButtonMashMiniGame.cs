using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class ButtonMashMiniGame : IMiniGame
    {
        public MiniGameType GameType => MiniGameType.ButtonMash;
        public EMiniGameState CurrentState { get; private set; } = EMiniGameState.Idle;
        public float NormalizedProgress => _currentGauge;
        public event System.Action<MiniGameResult> OnCompleted;

        // 유효한 입력이 들어올 때마다 발행 (UI 흔들림 연출 등에 사용)
        public event System.Action OnPressed;

        // 전체 제한 시간 대비 남은 시간 비율 (0~1).
        public float RemainingTimeRatio => _config != null && _config.TimeLimit > 0f
            ? Mathf.Clamp01(1f - _elapsedTime / _config.TimeLimit)
            : 0f;

        private readonly IInputProvider _input;
        private ButtonMashConfig _config;
        private float _currentGauge;
        private float _elapsedTime;
        private float _inputCooldown;
        private float _minInputInterval;

        public ButtonMashMiniGame(IInputProvider input)
        {
            _input = input;
        }
        
        // 게임 시작 시 초기화
        public void Begin(MiniGameConfig config)
        {
            _config = config as ButtonMashConfig;
            if (_config == null)
            {
                Debug.LogError("[ButtonMash] 잘못된 Config 타입이 전달됨");
                return;
            }

            _currentGauge = 0f;
            _elapsedTime = 0f;
            _inputCooldown = 0f;
            _minInputInterval = 1f / _config.MaxInputPerSecond;
            CurrentState = EMiniGameState.Playing;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != EMiniGameState.Playing)
            {
                return;
            }

            _elapsedTime += deltaTime;
            _inputCooldown -= deltaTime;

            // 자연 감소
            _currentGauge -= _config.DecayPerSecond * deltaTime;
            _currentGauge = Mathf.Max(0f, _currentGauge);

            // 입력 처리
            if (_input.GetKeyDown(_config.InputKey) && _inputCooldown <= 0f)
            {
                _currentGauge += _config.GainPerPress;
                _currentGauge = Mathf.Min(1f, _currentGauge);
                _inputCooldown = _minInputInterval;
                OnPressed?.Invoke();
            }

            // 게이지 100% 도달 시 즉시 성공
            if (_currentGauge >= 1f)
            {
                CurrentState = EMiniGameState.Succeeded;
                OnCompleted?.Invoke(new MiniGameResult(GameType, true, _elapsedTime));
                return;
            }

            // 시간 초과 시 실패
            if (_elapsedTime >= _config.TimeLimit)
            {
                CurrentState = EMiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
            }
        }
        
        // 게임 실패 처리
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
