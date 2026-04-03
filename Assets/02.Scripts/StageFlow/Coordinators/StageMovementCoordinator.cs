using System;
using UniRx;

namespace DontDillyDally.StageFlow
{
    // 현재 페이즈에 맞춰 플레이어 이동 가능 여부를 관리합니다.
    public sealed class StageMovementCoordinator : IDisposable
    {
        private readonly StageFlowRpcHandler _rpc;
        private IDisposable _phaseSubscription;

        public StageMovementCoordinator(StageFlowRpcHandler rpc)
        {
            _rpc = rpc;
        }

        // ── 구독 시작 / 해제 ─────────────────────────────────────────

        public void Initialize()
        {
            if (_rpc == null)
            {
                return;
            }

            _phaseSubscription?.Dispose();
            _phaseSubscription = _rpc.CurrentPhase.Subscribe(HandlePhaseChanged);
            PlayerRegistry.OnPlayerRegistered += HandlePlayerRegistered;
            HandlePhaseChanged(_rpc.CurrentPhase.Value);
        }

        public void Dispose()
        {
            _phaseSubscription?.Dispose();
            _phaseSubscription = null;
            PlayerRegistry.OnPlayerRegistered -= HandlePlayerRegistered;
        }

        // ── 내부 상태 반영 ───────────────────────────────────────────

        private void HandlePhaseChanged(EStagePhase phase)
        {
            bool canMove = phase == EStagePhase.Playing;
            SetAllPlayersMovementLocked(!canMove);
        }

        private void HandlePlayerRegistered(PlayerController player)
        {
            if (player == null || player.MovementAbility == null || _rpc == null)
            {
                return;
            }

            bool canMove = _rpc.CurrentPhase.Value == EStagePhase.Playing;
            player.MovementAbility.SetMovementLocked(!canMove);
        }

        private static void SetAllPlayersMovementLocked(bool locked)
        {
            foreach (PlayerController player in PlayerRegistry.GetAllPlayers())
            {
                if (player != null && player.MovementAbility != null)
                {
                    player.MovementAbility.SetMovementLocked(locked);
                }
            }
        }
    }
}
