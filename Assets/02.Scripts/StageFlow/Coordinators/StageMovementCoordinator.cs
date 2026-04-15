using System;
using UniRx;

namespace DontDillyDally.StageFlow
{
    // 현재 페이즈에 맞춰 플레이어 이동 가능 여부를 관리합니다.
    public sealed class StageMovementCoordinator : IDisposable
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly object _movementLockSource = new();
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
            bool canMove = CanMoveDuringPhase(phase);
            SetAllPlayersMovementLocked(!canMove);
        }

        private void HandlePlayerRegistered(PlayerController player)
        {
            if (player == null || player.MovementAbility == null || _rpc == null)
            {
                return;
            }

            bool canMove = CanMoveDuringPhase(_rpc.CurrentPhase.Value);
            player.MovementAbility.SetMovementLocked(_movementLockSource, !canMove);
        }

        private static bool CanMoveDuringPhase(EStagePhase phase)
        {
            return phase == EStagePhase.Playing ||
                   phase == EStagePhase.PatientTransition;
        }

        private void SetAllPlayersMovementLocked(bool locked)
        {
            foreach (PlayerController player in PlayerRegistry.GetAllPlayers())
            {
                if (player != null && player.MovementAbility != null)
                {
                    player.MovementAbility.SetMovementLocked(_movementLockSource, locked);
                }
            }
        }
    }
}
