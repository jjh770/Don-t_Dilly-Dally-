using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class DirectionQTEMiniGame : IMiniGame
    {
        public MiniGameType GameType => MiniGameType.DirectionQTE;
        public MiniGameState CurrentState { get; private set; } = MiniGameState.Idle;
        public event System.Action<MiniGameResult> OnCompleted;

        public int CurrentPromptIndex { get; private set; }
        public int TotalPrompts => _prompts?.Length ?? 0;
        public float CurrentPromptRemainingRatio => _currentTimeLimit > 0f ? _currentTimer / _currentTimeLimit : 0f;
        public Direction? CurrentDirection =>
            _prompts != null && CurrentPromptIndex < _prompts.Length
                ? _prompts[CurrentPromptIndex].Direction
                : null;

        public float NormalizedProgress =>
            TotalPrompts > 0 ? (float)_successCount / TotalPrompts : 0f;

        // UI 피드백용. null이면 아직 입력 없음.
        public bool? LastInputResult { get; private set; }

        private readonly IInputProvider _input;
        private DirectionQTEConfig _config;
        private QTEPrompt[] _prompts;
        private float _elapsedTime;
        private float _currentTimer;
        private float _currentTimeLimit;
        private int _successCount;
        private int _mistakeCount;
        private bool _waitingForInput;

        public DirectionQTEMiniGame(IInputProvider input)
        {
            _input = input;
        }

        public void Begin(MiniGameConfig config)
        {
            _config = config as DirectionQTEConfig;
            if (_config == null)
            {
                Debug.LogError("[DirectionQTE] 잘못된 Config 타입이 전달됨");
                return;
            }

            _prompts = GenerateSequence(_config.sequenceLength, _config.perInputTimeLimit);
            CurrentPromptIndex = 0;
            _successCount = 0;
            _mistakeCount = 0;
            _elapsedTime = 0f;
            LastInputResult = null;
            _waitingForInput = true;

            SetCurrentPromptTimer();
            CurrentState = MiniGameState.Playing;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != MiniGameState.Playing) return;

            _elapsedTime += deltaTime;
            _currentTimer -= deltaTime;

            // 현재 프롬프트 시간 초과
            if (_currentTimer <= 0f && _waitingForInput)
            {
                RegisterMistake();
                return;
            }

            if (!_waitingForInput) return;

            Direction? pressedDirection = ReadDirectionInput();
            if (pressedDirection == null) return;

            if (pressedDirection == _prompts[CurrentPromptIndex].Direction)
            {
                _successCount++;
                LastInputResult = true;
                AdvanceToNext();
            }
            else
            {
                RegisterMistake();
            }
        }

        public void Abort()
        {
            if (CurrentState != MiniGameState.Playing) return;
            CurrentState = MiniGameState.Failed;
            OnCompleted?.Invoke(new MiniGameResult(GameType, false, NormalizedProgress, _elapsedTime));
        }

        private static QTEPrompt[] GenerateSequence(int length, float perInputTime)
        {
            var prompts = new QTEPrompt[length];
            var values = (Direction[])System.Enum.GetValues(typeof(Direction));
            Direction? prev = null;
            int repeatCount = 0;

            for (int i = 0; i < length; i++)
            {
                Direction dir;
                do
                {
                    dir = values[Random.Range(0, values.Length)];
                }
                while (dir == prev && repeatCount >= 2);

                if (dir == prev) repeatCount++;
                else repeatCount = 1;

                prev = dir;
                prompts[i] = new QTEPrompt(dir, perInputTime);
            }
            return prompts;
        }

        private void SetCurrentPromptTimer()
        {
            if (CurrentPromptIndex < _prompts.Length)
            {
                _currentTimeLimit = _prompts[CurrentPromptIndex].TimeLimit;
                _currentTimer = _currentTimeLimit;
                _waitingForInput = true;
            }
        }

        private void AdvanceToNext()
        {
            CurrentPromptIndex++;
            if (CurrentPromptIndex >= _prompts.Length)
            {
                CompleteGame();
                return;
            }
            SetCurrentPromptTimer();
        }

        private void RegisterMistake()
        {
            _mistakeCount++;
            LastInputResult = false;
            _waitingForInput = false;

            if (_mistakeCount > _config.maxMistakes)
            {
                CurrentState = MiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, NormalizedProgress, _elapsedTime));
                return;
            }

            AdvanceToNext();
        }

        private void CompleteGame()
        {
            float accuracy = (float)_successCount / TotalPrompts;
            bool success = accuracy >= _config.successThreshold;
            CurrentState = success ? MiniGameState.Succeeded : MiniGameState.Failed;
            OnCompleted?.Invoke(new MiniGameResult(GameType, success, accuracy, _elapsedTime));
        }

        private Direction? ReadDirectionInput()
        {
            if (_input.GetKeyDown(KeyCode.UpArrow)) return Direction.Up;
            if (_input.GetKeyDown(KeyCode.DownArrow)) return Direction.Down;
            if (_input.GetKeyDown(KeyCode.LeftArrow)) return Direction.Left;
            if (_input.GetKeyDown(KeyCode.RightArrow)) return Direction.Right;
            return null;
        }
    }
}
