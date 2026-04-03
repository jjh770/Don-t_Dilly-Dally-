using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using DontDillyDally.MiniGame;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 레시피 정답 뒤 미니게임의 실행 결과를 환자 상태와 긴급 이벤트 흐름에 반영합니다.
    public sealed class StageMiniGameResolutionCoordinator
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly StageMiniGameCoordinator _miniGameCoordinator;
        private readonly EmergencyEventPolicy _emergencyPolicy;
        private readonly StageEmergencyCoordinator _emergencyCoordinator;
        private readonly IStageMiniGameResolutionHost _host;
        private readonly float _failPenalty;
        private readonly float _successHeal;

        public StageMiniGameResolutionCoordinator(
            StageFlowRpcHandler rpc,
            StageMiniGameCoordinator miniGameCoordinator,
            EmergencyEventPolicy emergencyPolicy,
            StageEmergencyCoordinator emergencyCoordinator,
            IStageMiniGameResolutionHost host,
            float failPenalty,
            float successHeal)
        {
            _rpc = rpc;
            _miniGameCoordinator = miniGameCoordinator;
            _emergencyPolicy = emergencyPolicy;
            _emergencyCoordinator = emergencyCoordinator;
            _host = host;
            _failPenalty = failPenalty;
            _successHeal = successHeal;
        }

        // ── 미니게임 결과 후처리 ─────────────────────────────────────

        public async UniTask<bool> RunRecipeMiniGame(CancellationToken ct)
        {
            MiniGameType type = MiniGameTypeExtensions.GetRandom();
            int surgeonActorNumber = GetMiniGameTargetActorNumber();
            Debug.Log($"[StageFlow]     레시피 미니게임 시작: {type} | 집도의 Actor {surgeonActorNumber}");

            bool success = _miniGameCoordinator != null &&
                await _miniGameCoordinator.RunRecipeMiniGame(surgeonActorNumber, type, ct);
            Debug.Log($"[StageFlow]     레시피 미니게임 결과: {(success ? "성공" : "실패")}");

            if (success)
            {
                float recoveredHealth = _host != null ? _host.ApplyHeal(_successHeal) : 0f;
                Debug.Log($"[StageFlow]     미니게임 성공! 체력 +{_successHeal} | 현재 체력: {recoveredHealth}");
                EventManager.Instance?.OnSurgerySuccess();
                return true;
            }

            float newHealth = _host != null ? _host.ApplyDamage(_failPenalty) : 0f;
            Debug.Log($"[StageFlow]     미니게임 실패! 체력 -{_failPenalty} | 현재 체력: {newHealth}");
            EventManager.Instance?.OnSurgeryFail(SurgeryFailureReason.MiniGameFailure);

            if (_host != null && _host.IsGameOver)
            {
                Debug.Log("[StageFlow]     환자 사망으로 게임 오버 처리");
                return false;
            }

            if (_emergencyPolicy != null && _emergencyPolicy.ShouldTriggerOnRecipeFail())
            {
                Debug.Log("[StageFlow]     미니게임 실패로 긴급 이벤트를 시작합니다.");

                if (_emergencyCoordinator != null &&
                    _emergencyCoordinator.TryStartEmergencyEvent(
                        EmergencyTriggerSource.MiniGameFail,
                        _host != null ? _host.CurrentPhase : EStagePhase.None,
                        _host != null && _host.IsGameOver,
                        _host != null && _host.IsWaitingForRecipeSubmission))
                {
                    await _emergencyCoordinator.WaitForResult(ct);
                }
            }

            return false;
        }

        // ── 내부 보조 메서드 ─────────────────────────────────────────

        private int GetMiniGameTargetActorNumber()
        {
            if (_rpc != null && _rpc.SurgeonActorNumber.Value > 0)
            {
                return _rpc.SurgeonActorNumber.Value;
            }

            if (Photon.Pun.PhotonNetwork.LocalPlayer != null)
            {
                return Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
            }

            return -1;
        }
    }
}
