using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 게임 오버와 스테이지 보상 정산을 전담합니다.
    public sealed class StageOutcomeCoordinator : IDisposable
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly StageRpcAckCoordinator _ackCoordinator;
        private readonly IStageOutcomeHost _host;
        private readonly RewardLLMEvaluator _rewardEvaluator;
        private readonly RewardMoneyPolicy _moneyPolicy;
        private readonly RewardSettlementService _rewardSettlementService;

        public StageOutcomeCoordinator(
            StageFlowRpcHandler rpc,
            StageRpcAckCoordinator ackCoordinator,
            IStageOutcomeHost host,
            RewardLLMEvaluator rewardEvaluator,
            RewardMoneyPolicy moneyPolicy,
            RewardSettlementService rewardSettlementService)
        {
            _rpc = rpc;
            _ackCoordinator = ackCoordinator;
            _host = host;
            _rewardEvaluator = rewardEvaluator;
            _moneyPolicy = moneyPolicy;
            _rewardSettlementService = rewardSettlementService;

            if (_rpc != null)
            {
                _rpc.OnGameOverReceived += HandleGameOverReceived;
                _rpc.OnStageRewardGrantedReceived += HandleStageRewardGrantedReceived;
            }
        }

        // ── 공개 흐름 진입점 ─────────────────────────────────────────

        public bool TryTriggerGameOver(
            EGameOverReason reason,
            CancellationTokenSource flowCts,
            float returnToWaitingRoomDelaySec,
            int gameOverAckTotalTimeoutMs,
            int ackTimeoutMs)
        {
            if (_host != null && _host.IsGameOver)
            {
                return false;
            }

            _host?.MarkGameOver();

            Debug.Log($"[StageFlow] ========== 게임 오버: {reason} ==========");

            flowCts?.Cancel();
            _host?.PauseDrain();
            _host?.PauseStageTimer();
            _host?.SyncTimerState();

            _rpc?.SetPhase(EStagePhase.GameOver);
            _host?.PublishGameOver(reason);
            DiseaseData disease = null;
            StageFlowManager.Instance?.TryGetCurrentDisease(out disease);
            Player player = GetSurgeonPlayer();

            if (reason == EGameOverReason.PlayerDisconnected)
            {
                NotifyUIService.QueueForNextScene(ENotifyType.OtherPlayerLeft);
            }
            else if (reason == EGameOverReason.PatientDeath)
            {
                StageFlowManager.Instance?.PerformanceTracker.Record(player, disease, EPerformanceEventType.PatientDied);
                EventManager.Instance?.OnPatientDeath();
            }
            else if (reason == EGameOverReason.TimeExpired)
            {
                StageFlowManager.Instance?.PerformanceTracker.Record(player, disease, EPerformanceEventType.Timeout);
                EventManager.Instance?.OnTimeOut();
            } 

            float remainingTime = _host != null ? _host.RemainingTime : 0f;
            Debug.Log($"[StageFlow] 게임 오버: {reason} | 남은 시간: {remainingTime:F1}초");

            _host?.ClearRoles();
            BroadcastGameOverAndRewardAsync(
                reason,
                returnToWaitingRoomDelaySec,
                gameOverAckTotalTimeoutMs,
                ackTimeoutMs).Forget();
            return true;
        }

        public async UniTask ApplyReward()
        {
            if (!PhotonServerManager.Instance.IsMasterClient)
            {
                return;
            }

            StageRuntimeData stageData = _host?.StageData;
            if (stageData == null || _rpc == null)
            {
                return;
            }

            var result = new StageResult(
                savedCount: stageData.SavedCount,
                patientCount: stageData.Settings.PatientSettings.PatientCount,
                difficulty: stageData.Settings.PatientSettings.Difficulty);

            StageStars previousStars = RoomDataManager.Instance.GetStageStars(stageData.StageId);
            int beforeCoin = RoomDataManager.Instance.Coin.Value;
            int beforeStar = RoomDataManager.Instance.Star;
            RewardNarrativeResult narrative = await _rewardEvaluator.EvaluateAsync(
                StageFlowManager.Instance.PerformanceTracker.Events,
                stageData.SavedCount,
                stageData.Settings.PatientSettings.PatientCount,
                _host?.IsGameOver ?? false,
                beforeCoin);

            RewardMoneyAdjustment finalAdjustment = narrative.HasRequestedMoneyDelta
                ? _moneyPolicy.ApplyRequestedDelta(narrative.requestedMoneyDelta, beforeCoin)
                : _moneyPolicy.ApplyRequestedDelta(0, beforeCoin);

            StageReward finalReward = _rewardSettlementService.Build(
                result,
                previousStars,
                finalAdjustment,
                narrative);

            finalReward = RoomDataManager.Instance.ApplyReward(stageData.StageId, finalReward);
            var settlement = new StageRewardSettlement(
                finalReward,
                result,
                beforeCoin,
                RoomDataManager.Instance.Coin.Value,
                beforeStar,
                RoomDataManager.Instance.Star);

            _rpc.BroadcastStageReward(settlement);

            Debug.Log($"[StageFlow] 보상 정산 => 별: {finalReward.Stars}, 코인: {finalReward.Money - finalReward.MoneyDelta} ({finalReward.MoneyDelta}), 최고기록 갱신: {finalReward.IsNewBest}");
            Debug.Log(finalReward.SummaryText);
        }

        // ── 정리 ────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_rpc != null)
            {
                _rpc.OnGameOverReceived -= HandleGameOverReceived;
                _rpc.OnStageRewardGrantedReceived -= HandleStageRewardGrantedReceived;
            }
        }

        // ── 내부 완료 / 수신 처리 ────────────────────────────────────

        private async UniTaskVoid BroadcastGameOverAndRewardAsync(
            EGameOverReason reason,
            float returnToWaitingRoomDelaySec,
            int gameOverAckTotalTimeoutMs,
            int ackTimeoutMs)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(gameOverAckTotalTimeoutMs);

            try
            {
                if (_ackCoordinator != null && _rpc != null)
                {
                    bool gameOverAckCompleted = await _ackCoordinator.BroadcastAndWaitAck(
                        () => _rpc.BroadcastGameOver(reason),
                        handler => _rpc.OnGameOverAckReceived += handler,
                        handler => _rpc.OnGameOverAckReceived -= handler,
                        "게임 오버",
                        ackTimeoutMs,
                        cts.Token);

                    if (!gameOverAckCompleted)
                    {
                        Debug.LogWarning("[StageFlow] 게임 오버 ACK를 모두 받지 못한 채 보상 단계로 진행합니다.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            if (EGameOverReason.PlayerDisconnected == reason)
            {
                Debug.Log("[StageFlow] Player가 게임을 이탈해 대기실로 복귀합니다.");
                PhotonServerManager.Instance?.ReturnWaitingRoom();
                return;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(returnToWaitingRoomDelaySec));
            await ApplyReward();
        }

        private void HandleGameOverReceived(EGameOverReason reason)
        {
            _host?.PublishGameOver(reason);

            if (reason == EGameOverReason.PlayerDisconnected)
            {
                NotifyUIService.QueueForNextScene(ENotifyType.OtherPlayerLeft);
            }
            else if (reason == EGameOverReason.PatientDeath)
            {
                EventManager.Instance?.OnPatientDeath();
            }
            else if (reason == EGameOverReason.TimeExpired)
            {
                EventManager.Instance?.OnTimeOut();
            }
        }

        private void HandleStageRewardGrantedReceived(StageRewardSettlement settlement)
        {
            _host?.PublishReward(settlement);
        }

        private Player GetSurgeonPlayer()
        {
            int actorNumber = StageFlowManager.Instance != null
                ? StageFlowManager.Instance.SurgeonActorNumber.Value
                : -1;

            if (PhotonServerManager.Instance != null &&
                PhotonServerManager.Instance.TryGetPlayerByActorNumber(actorNumber, out Player player))
            {
                return player;
            }

            return null;
        }
    }
}
