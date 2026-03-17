using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class PrecisionStopMiniGame : IMiniGame
    {
        private const float RoundTransitionDelay = 0.6f;

        public MiniGameType GameType => MiniGameType.PrecisionStop;
        public MiniGameState CurrentState { get; private set; } = MiniGameState.Idle;
        public event System.Action<MiniGameResult> OnCompleted;

        public float CursorPosition { get; private set; }
        public float TargetZoneCenter { get; private set; }
        public float TargetZoneWidth => _config?.targetZoneWidth ?? 0f;
        public int CurrentRound { get; private set; }
        public int TotalRounds => _config?.roundCount ?? 0;
        public int SuccessfulRounds { get; private set; }
        public float NormalizedProgress =>
            TotalRounds > 0 ? (float)CurrentRound / TotalRounds : 0f;

        /// <summary>UI 표시용 라운드 번호 (1-based). 쿨다운 중에는 완료한 라운드를 표시.</summary>
        public int DisplayRound => _isRoundActive ? CurrentRound + 1 : CurrentRound;

        /// <summary>전체 제한 시간 대비 남은 시간 비율 (0~1)</summary>
        public float RemainingTimeRatio => _config != null && _config.timeLimit > 0f
            ? Mathf.Clamp01((_config.timeLimit - _elapsedTime) / _config.timeLimit)
            : 0f;

        // UI 연출용. 라운드 전환 딜레이 동안 이전 결과를 유지.
        public bool? LastRoundResult { get; private set; }

        private readonly IInputProvider _input;
        private PrecisionStopConfig _config;
        private float _elapsedTime;
        private float _currentSpeed;
        private int _movingDirection = 1;
        private bool _isRoundActive;
        private float _roundCooldown;

        public PrecisionStopMiniGame(IInputProvider input)
        {
            _input = input;
        }

        public void Begin(MiniGameConfig config)
        {
            _config = config as PrecisionStopConfig;
            if (_config == null)
            {
                Debug.LogError("[PrecisionStop] 잘못된 Config 타입이 전달됨");
                return;
            }

            CurrentRound = 0;
            SuccessfulRounds = 0;
            _elapsedTime = 0f;
            LastRoundResult = null;
            _currentSpeed = _config.cursorSpeed;

            StartNewRound();
            CurrentState = MiniGameState.Playing;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != MiniGameState.Playing) return;

            _elapsedTime += deltaTime;

            // 전체 시간 초과 → 실패
            if (_config.timeLimit > 0f && _elapsedTime >= _config.timeLimit)
            {
                CurrentState = MiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, NormalizedProgress, _elapsedTime));
                return;
            }

            // 라운드 전환 대기 중
            if (!_isRoundActive)
            {
                _roundCooldown -= deltaTime;
                if (_roundCooldown <= 0f)
                {
                    StartNewRound();
                }
                return;
            }

            // 커서 이동 (핑퐁)
            CursorPosition += _movingDirection * _currentSpeed * deltaTime;
            if (CursorPosition >= 1f)
            {
                CursorPosition = 1f;
                _movingDirection = -1;
            }
            else if (CursorPosition <= 0f)
            {
                CursorPosition = 0f;
                _movingDirection = 1;
            }

            // 정지 입력
            if (_input.GetKeyDown(_config.inputKey))
            {
                EvaluateStop();
            }
        }

        public void Abort()
        {
            if (CurrentState != MiniGameState.Playing) return;
            CurrentState = MiniGameState.Failed;
            OnCompleted?.Invoke(new MiniGameResult(GameType, false, NormalizedProgress, _elapsedTime));
        }

        private void StartNewRound()
        {
            float padding = _config.targetZonePadding;
            float halfWidth = _config.targetZoneWidth * 0.5f;
            float minCenter = padding + halfWidth;
            float maxCenter = 1f - padding - halfWidth;
            TargetZoneCenter = Random.Range(minCenter, maxCenter);

            CursorPosition = 0f;
            _movingDirection = 1;
            _isRoundActive = true;

            // 2라운드부터 속도 증가
            if (CurrentRound > 0)
            {
                _currentSpeed *= _config.speedMultiplierPerRound;
            }
        }

        private void EvaluateStop()
        {
            _isRoundActive = false;
            float halfWidth = _config.targetZoneWidth * 0.5f;
            float min = TargetZoneCenter - halfWidth;
            float max = TargetZoneCenter + halfWidth;

            bool hit = CursorPosition >= min && CursorPosition <= max;
            LastRoundResult = hit;
            CurrentRound++;

            if (!hit)
            {
                // 한 번이라도 실패하면 즉시 게임 오버
                CurrentState = MiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, NormalizedProgress, _elapsedTime));
                return;
            }

            SuccessfulRounds++;

            if (CurrentRound >= _config.roundCount)
            {
                // 전부 성공
                CurrentState = MiniGameState.Succeeded;
                OnCompleted?.Invoke(new MiniGameResult(GameType, true, 1f, _elapsedTime));
            }
            else
            {
                _roundCooldown = RoundTransitionDelay;
            }
        }
    }
}
