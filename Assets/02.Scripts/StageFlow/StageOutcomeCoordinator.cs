using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using System;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 게임 오버와 스테이지 보상 정산을 전담합니다.
    public sealed class StageOutcomeCoordinator : IDisposable
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly StageRpcAckCoordinator _ackCoordinator;
        private readonly IStageOutcomeHost _host;

        public StageOutcomeCoordinator(
            StageFlowRpcHandler rpc,
            StageRpcAckCoordinator ackCoordinator,
            IStageOutcomeHost host)
        {
            _rpc = rpc;
            _ackCoordinator = ackCoordinator;
            _host = host;

            if (_rpc != null)
            {
                _rpc.OnGameOverReceived += HandleGameOverReceived;
                _rpc.OnStageRewardGrantedReceived += HandleStageRewardGrantedReceived;
            }
        }

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

            if (reason == EGameOverReason.PatientDeath)
            {
                EventManager.Instance?.OnPatientDeath();
            }
            else if (reason == EGameOverReason.TimeExpired)
            {
                EventManager.Instance?.OnTimeOut();
            }

            float remainingTime = _host != null ? _host.RemainingTime : 0f;
            Debug.Log($"[StageFlow] 게임 오버: {reason} | 남은 타이머: {remainingTime:F1}초 | 5초 후 대기실 복귀 가능");

            _host?.ClearRoles();
            BroadcastGameOverAndRewardAsync(
                reason,
                returnToWaitingRoomDelaySec,
                gameOverAckTotalTimeoutMs,
                ackTimeoutMs).Forget();
            return true;
        }

        public void ApplyReward()
        {
            StageRuntimeData stageData = _host?.StageData;
            if (stageData == null || _rpc == null)
            {
                return;
            }

            var result = new StageResult(
                savedCount: stageData.SavedCount,
                patientCount: stageData.PatientCount,
                difficulty: stageData.Difficulty);

            StageReward reward = RoomDataManager.Instance.ApplyReward(stageData.StageId, result);
            _rpc.BroadcastStageReward(reward, result);

            Debug.Log($"별: {reward.Stars} / 돈: {reward.Money} / 신기록: {reward.IsNewBest}");
        }

        public void Dispose()
        {
            if (_rpc != null)
            {
                _rpc.OnGameOverReceived -= HandleGameOverReceived;
                _rpc.OnStageRewardGrantedReceived -= HandleStageRewardGrantedReceived;
            }
        }

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
                    await _ackCoordinator.BroadcastAndWaitAck(
                        () => _rpc.BroadcastGameOver(reason),
                        handler => _rpc.OnGameOverAckReceived += handler,
                        handler => _rpc.OnGameOverAckReceived -= handler,
                        "게임 오버",
                        ackTimeoutMs,
                        cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[StageFlow] 게임 오버 ACK 대기 중 타임아웃");
            }

            await UniTask.Delay(TimeSpan.FromSeconds(returnToWaitingRoomDelaySec));
            ApplyReward();
        }

        private void HandleGameOverReceived(EGameOverReason reason)
        {
            _host?.PublishGameOver(reason);
        }

        private void HandleStageRewardGrantedReceived(StageReward reward, StageResult result)
        {
            _host?.PublishReward(reward, result);
        }
    }
}
