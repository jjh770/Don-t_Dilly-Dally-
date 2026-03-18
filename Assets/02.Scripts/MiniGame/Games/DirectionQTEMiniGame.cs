using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class DirectionQTEMiniGame : IMiniGame
    {
        public MiniGameType GameType => MiniGameType.DirectionQTE;
        public EMiniGameState CurrentState { get; private set; } = EMiniGameState.Idle;
        public event System.Action<MiniGameResult> OnCompleted;

        public int CurrentPromptIndex { get; private set; }
        public int TotalPrompts => _prompts?.Length ?? 0;

        // 전체 시퀀스 (UI에서 한번에 표시용).
        public QTEPrompt[] Prompts => _prompts;

        // 전체 제한 시간 대비 남은 시간 비율 (0~1).
        public float RemainingTimeRatio => _timeLimit > 0f ? Mathf.Clamp01(_remainingTime / _timeLimit) : 0f;

        public float NormalizedProgress =>
            TotalPrompts > 0 ? (float)CurrentPromptIndex / TotalPrompts : 0f;

        // UI 피드백용, null이면 아직 입력 없음.
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
        
        // 게임 시작 시 초기화
        public void Begin(MiniGameConfig config)
        {
            _config = config as DirectionQTEConfig;
            if (_config == null)
            {
                Debug.LogError("[DirectionQTE] 잘못된 Config 타입이 전달됨");
                return;
            }

            _prompts = GenerateSequence(_config.SequenceLength);
            CurrentPromptIndex = 0;
            _elapsedTime = 0f;
            _timeLimit = _config.TimeLimit;
            _remainingTime = _timeLimit;
            LastInputResult = null;

            CurrentState = EMiniGameState.Playing;
        }
        
        // 매 프레임마다 입력과 시간 체크
        public void Tick(float deltaTime)
        {
            if (CurrentState != EMiniGameState.Playing) return;

            _elapsedTime += deltaTime;
            _remainingTime -= deltaTime;

            // 전체 시간 초과 → 실패
            if (_remainingTime <= 0f)
            {
                CurrentState = EMiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
                return;
            }

            EQteDirection? pressedDirection = ReadDirectionInput();
            if (pressedDirection == null) return;

            if (pressedDirection == _prompts[CurrentPromptIndex].EQteDirection)
            {
                // 정답
                LastInputResult = true;
                CurrentPromptIndex++;

                // 전부 맞추면 즉시 성공
                if (CurrentPromptIndex >= _prompts.Length)
                {
                    CurrentState = EMiniGameState.Succeeded;
                    OnCompleted?.Invoke(new MiniGameResult(GameType, true, _elapsedTime));
                }
            }
            else
            {
                // 오답 → 즉시 실패
                LastInputResult = false;
                CurrentState = EMiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
            }
        }
        
        // 게임 실패 처리
        public void Abort()
        {
            if (CurrentState != EMiniGameState.Playing) return;
            CurrentState = EMiniGameState.Failed;
            OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
        }
        
        // 시퀀스 생성: 같은 방향이 3회 이상 연속되지 않도록
        private static QTEPrompt[] GenerateSequence(int length)
        {
            var prompts = new QTEPrompt[length];
            var values = (EQteDirection[])System.Enum.GetValues(typeof(EQteDirection));
            EQteDirection? prev = null;
            int repeatCount = 0;

            for (int i = 0; i < length; i++)
            {
                EQteDirection dir;
                do
                {
                    dir = values[Random.Range(0, values.Length)];
                }
                while (dir == prev && repeatCount >= 2);

                if (dir == prev) repeatCount++;
                else repeatCount = 1;

                prev = dir;
                prompts[i] = new QTEPrompt(dir);
            }
            return prompts;
        }

        private EQteDirection? ReadDirectionInput()
        {
            if (_input.GetKeyDown(KeyCode.UpArrow)) return EQteDirection.Up;
            if (_input.GetKeyDown(KeyCode.DownArrow)) return EQteDirection.Down;
            if (_input.GetKeyDown(KeyCode.LeftArrow)) return EQteDirection.Left;
            if (_input.GetKeyDown(KeyCode.RightArrow)) return EQteDirection.Right;
            return null;
        }
    }
}
