using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class PrecisionStopMiniGame : IMiniGame
    {
        private const float RoundTransitionDelay = 0.6f;

        public MiniGameType GameType => MiniGameType.PrecisionStop;
        public EMiniGameState CurrentState { get; private set; } = EMiniGameState.Idle;
        public event System.Action<MiniGameResult> OnCompleted;

        public float CursorPosition { get; private set; }
        public float TargetZoneCenter { get; private set; }
        public float TargetZoneWidth => _config?.TargetZoneWidth ?? 0f;
        public int CurrentRound { get; private set; }
        public int TotalRounds => _config?.RoundCount ?? 0;
        public int SuccessfulRounds { get; private set; }
        public float NormalizedProgress =>
            TotalRounds > 0 ? (float)CurrentRound / TotalRounds : 0f;

        // UI 표시용 라운드 번호 (1-based). 쿨다운 중에는 완료한 라운드를 표시.
        public int DisplayRound => _isRoundActive ? CurrentRound + 1 : CurrentRound;

        // 전체 제한 시간 대비 남은 시간 비율 (0~1).
        public float RemainingTimeRatio => _config != null && _config.TimeLimit > 0f
            ? Mathf.Clamp01((_config.TimeLimit - _elapsedTime) / _config.TimeLimit)
            : 0f;

        // UI 연출용, 라운드 전환 딜레이 동안 이전 결과를 유지.
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

        // 게임 시작 시 초기화
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
            _currentSpeed = _config.CursorSpeed;

            StartNewRound();
            CurrentState = EMiniGameState.Playing;
        }

        // 매 프레임마다 입력과 시간 체크
        public void Tick(float deltaTime)
        {
            if (CurrentState != EMiniGameState.Playing) return;

            _elapsedTime += deltaTime;

            // 전체 시간 초과 → 실패
            if (_config.TimeLimit > 0f && _elapsedTime >= _config.TimeLimit)
            {
                CurrentState = EMiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
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
            if (_input.GetKeyDown(_config.InputKey))
            {
                EvaluateStop();
            }
        }

        // 게임 실패 처리
        public void Abort()
        {
            if (CurrentState != EMiniGameState.Playing) return;
            CurrentState = EMiniGameState.Failed;
            OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
        }

        private void StartNewRound()
        {
            float padding = _config.TargetZonePadding;
            float halfWidth = _config.TargetZoneWidth * 0.5f;
            float minCenter = padding + halfWidth;
            float maxCenter = 1f - padding - halfWidth;
            TargetZoneCenter = Random.Range(minCenter, maxCenter);

            CursorPosition = 0f;
            _movingDirection = 1;
            _isRoundActive = true;

            // 2라운드부터 속도 증가
            if (CurrentRound > 0)
            {
                _currentSpeed *= _config.SpeedMultiplierPerRound;
            }
        }

        // 플레이어가 정지 입력을 했을 때 결과 평가
        private void EvaluateStop()
        {
            _isRoundActive = false;
            float halfWidth = _config.TargetZoneWidth * 0.5f;
            float min = TargetZoneCenter - halfWidth;
            float max = TargetZoneCenter + halfWidth;

            bool hit = CursorPosition >= min && CursorPosition <= max;
            LastRoundResult = hit;
            CurrentRound++;

            if (!hit)
            {
                // 한 번이라도 실패하면 즉시 게임 오버
                CurrentState = EMiniGameState.Failed;
                OnCompleted?.Invoke(new MiniGameResult(GameType, false, _elapsedTime));
                return;
            }

            SuccessfulRounds++;

            if (CurrentRound >= _config.RoundCount)
            {
                // 전부 성공
                CurrentState = EMiniGameState.Succeeded;
                OnCompleted?.Invoke(new MiniGameResult(GameType, true, _elapsedTime));
            }
            else
            {
                _roundCooldown = RoundTransitionDelay;
            }
        }
    }
}
