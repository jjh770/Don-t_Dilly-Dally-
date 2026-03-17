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

        /// <summary>전체 시퀀스 (UI에서 한번에 표시용)</summary>
        public QTEPrompt[] Prompts => _prompts;

        /// <summary>전체 제한 시간 대비 남은 시간 비율 (0~1)</summary>
        public float RemainingTimeRatio => _timeLimit > 0f ? Mathf.Clamp01(_remainingTime / _timeLimit) : 0f;

        public float NormalizedProgress =>
            TotalPrompts > 0 ? (float)CurrentPromptIndex / TotalPrompts : 0f;

        // UI 피드백용. null이면 아직 입력 없음.
        public bool? LastInputResult { get; private set; }

        private readonly IInputProvider _input;
        private DirectionQTEConfig _config;
        private QTEPrompt[] _prompts;
        private float _elapsedTime;
        private float _timeLimit;
        private float _remainingTime;

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

            _prompts = GenerateSequence(_config.sequenceLength);
            CurrentPromptIndex = 0;
            _elapsedTime = 0f;
            _timeLimit = _config.timeLimit;
            _remainingTime = _timeLimit;
            LastInputResult = null;

            CurrentState = MiniGameState.Playing;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != MiniGameState.Playing) return;

            _elapsedTime += deltaTime;
            _remainingTime -= deltaTime;

            // 전체 시간 초과 → 실패
            if (_remainingTime <= 0f)
            {
                CurrentState = MiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, NormalizedProgress, _elapsedTime));
                return;
            }

            Direction? pressedDirection = ReadDirectionInput();
            if (pressedDirection == null) return;

            if (pressedDirection == _prompts[CurrentPromptIndex].Direction)
            {
                // 정답
                LastInputResult = true;
                CurrentPromptIndex++;

                // 전부 맞추면 즉시 성공
                if (CurrentPromptIndex >= _prompts.Length)
                {
                    CurrentState = MiniGameState.Succeeded;
                    OnCompleted?.Invoke(new MiniGameResult(GameType, true, 1f, _elapsedTime));
                }
            }
            else
            {
                // 오답 → 즉시 실패
                LastInputResult = false;
                CurrentState = MiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, NormalizedProgress, _elapsedTime));
            }
        }

        public void Abort()
        {
            if (CurrentState != MiniGameState.Playing) return;
            CurrentState = MiniGameState.Failed;
            OnCompleted?.Invoke(new MiniGameResult(GameType, false, NormalizedProgress, _elapsedTime));
        }

        private static QTEPrompt[] GenerateSequence(int length)
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
                prompts[i] = new QTEPrompt(dir, 0f); // timeLimit per prompt은 미사용
            }
            return prompts;
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
