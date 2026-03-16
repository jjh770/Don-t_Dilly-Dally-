using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class ButtonMashMiniGame : IMiniGame
    {
        public MiniGameType GameType => MiniGameType.ButtonMash;
        public MiniGameState CurrentState { get; private set; } = MiniGameState.Idle;
        public float NormalizedProgress => _currentGauge;
        public event System.Action<MiniGameResult> OnCompleted;

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
            _minInputInterval = 1f / _config.maxInputPerSecond;
            CurrentState = MiniGameState.Playing;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != MiniGameState.Playing) return;

            _elapsedTime += deltaTime;
            _inputCooldown -= deltaTime;

            // 자연 감소
            _currentGauge -= _config.decayPerSecond * deltaTime;
            _currentGauge = Mathf.Max(0f, _currentGauge);

            // 입력 처리
            if (_input.GetKeyDown(_config.inputKey) && _inputCooldown <= 0f)
            {
                _currentGauge += _config.gainPerPress;
                _currentGauge = Mathf.Min(1f, _currentGauge);
                _inputCooldown = _minInputInterval;
            }

            // 시간 초과 판정
            if (_elapsedTime >= _config.timeLimit)
            {
                bool success = _currentGauge >= _config.successThreshold;
                CurrentState = success ? MiniGameState.Succeeded : MiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, success, _currentGauge, _elapsedTime));
            }
        }

        public void Abort()
        {
            if (CurrentState != MiniGameState.Playing) return;

            CurrentState = MiniGameState.Failed;
            OnCompleted?.Invoke(new MiniGameResult(GameType, false, _currentGauge, _elapsedTime));
        }
    }
}
