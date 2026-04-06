using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using DontDillyDally.MiniGame;
using Photon.Pun;
using Photon.Realtime;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 레시피 미니게임 결과에 따른 체력 반영과 긴급 이벤트 연계를 처리합니다.
    public sealed class StageMiniGameResolutionCoordinator
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly IStageMiniGameResolutionHost _host;
        private readonly IStageMiniGameResolutionDependencies _dependencies;

        public StageMiniGameResolutionCoordinator(
            StageFlowRpcHandler rpc,
            IStageMiniGameResolutionHost host,
            IStageMiniGameResolutionDependencies dependencies)
        {
            _rpc = rpc;
            _host = host;
            _dependencies = dependencies;
        }

        // 레시피 미니게임을 실행하고 결과에 따라 후속 처리를 수행합니다.
        public async UniTask<bool> RunRecipeMiniGame(CancellationToken ct)
        {
            MiniGameType type = MiniGameTypeExtensions.GetRandom();
            int surgeonActorNumber = GetMiniGameTargetActorNumber();
            Debug.Log($"[StageFlow] 레시피 미니게임 시작: {type} | 집도의 Actor {surgeonActorNumber}");

            bool success = _dependencies?.MiniGameCoordinator != null &&
                await _dependencies.MiniGameCoordinator.RunRecipeMiniGame(surgeonActorNumber, type, ct);
            Debug.Log($"[StageFlow] 레시피 미니게임 결과: {(success ? "성공" : "실패")}");

            DiseaseData disease = null;
            StageFlowManager.Instance?.TryGetCurrentDisease(out disease);
            Player player = GetMiniGameTargetPlayer();

            if (success)
            {
                float recoveredHealth = _host != null ? _host.ApplyHeal(_dependencies?.MiniGameSuccessHeal ?? 0f) : 0f;
                Debug.Log($"[StageFlow] 미니게임 성공. 체력 +{_dependencies?.MiniGameSuccessHeal ?? 0f} | 현재 체력: {recoveredHealth}");
                StageFlowManager.Instance?.PerformanceTracker.Record(player, disease, EPerformanceEventType.MiniGameSuccess);
                EventManager.Instance?.OnSurgerySuccess();
                return true;
            }

            float newHealth = _host != null ? _host.ApplyDamage(_dependencies?.MiniGameFailPenalty ?? 0f) : 0f;
            Debug.Log($"[StageFlow] 미니게임 실패. 체력 -{_dependencies?.MiniGameFailPenalty ?? 0f} | 현재 체력: {newHealth}");
            StageFlowManager.Instance?.PerformanceTracker.Record(player, disease, EPerformanceEventType.MiniGameFail);
            EventManager.Instance?.OnSurgeryFail(SurgeryFailureReason.MiniGameFailure);

            if (_host != null && _host.IsGameOver)
            {
                Debug.Log("[StageFlow] 미니게임 결과 처리 중 환자가 사망했습니다.");
                return false;
            }

            if (_dependencies?.EmergencyPolicy != null &&
                _dependencies.EmergencyPolicy.ShouldTriggerOnMiniGameFail(_host?.StageData))
            {
                Debug.Log("[StageFlow] 미니게임 실패로 긴급 이벤트를 시작합니다.");

                if (_dependencies.EmergencyCoordinator != null &&
                    _dependencies.EmergencyCoordinator.TryStartEmergencyEvent(
                        EmergencyTriggerSource.MiniGameFail,
                        _host != null ? _host.CurrentPhase : EStagePhase.None,
                        _host != null && _host.IsGameOver,
                        _host != null && _host.IsWaitingForRecipeSubmission))
                {
                    await _dependencies.EmergencyCoordinator.WaitForResult(ct);
                }
            }

            return false;
        }

        private int GetMiniGameTargetActorNumber()
        {
            if (_rpc != null && _rpc.SurgeonActorNumber.Value > 0)
            {
                return _rpc.SurgeonActorNumber.Value;
            }

            if (PhotonNetwork.LocalPlayer != null)
            {
                return PhotonNetwork.LocalPlayer.ActorNumber;
            }

            return -1;
        }

        private Player GetMiniGameTargetPlayer()
        {
            int actorNumber = GetMiniGameTargetActorNumber();

            if (PhotonServerManager.Instance != null &&
                PhotonServerManager.Instance.TryGetPlayerByActorNumber(actorNumber, out Player player))
            {
                return player;
            }

            return null;
        }
    }
}
