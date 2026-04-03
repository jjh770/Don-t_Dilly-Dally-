using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using Photon.Pun;
using System;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 스테이지 시작 전 surgeon, stage data, countdown 동기화를 전담합니다.
    public sealed class StageBootstrapCoordinator : IDisposable
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly StageRpcAckCoordinator _ackCoordinator;
        private readonly Action<StageRuntimeData> _onStageDataReceived;

        public StageBootstrapCoordinator(
            StageFlowRpcHandler rpc,
            StageRpcAckCoordinator ackCoordinator,
            Action<StageRuntimeData> onStageDataReceived)
        {
            _rpc = rpc;
            _ackCoordinator = ackCoordinator;
            _onStageDataReceived = onStageDataReceived;

            if (_rpc != null)
            {
                _rpc.OnStageDataReceived += HandleStageDataReceived;
                if (_rpc.TryGetLatestStageData(out StageRuntimeData latestStageData))
                {
                    HandleStageDataReceived(latestStageData);
                }
            }
        }

        // ── 시작 동기화 ──────────────────────────────────────────────

        public async UniTask SynchronizeStageStart(
            StageRuntimeData stageData,
            int surgeonActorNumber,
            float countdownSeconds,
            int ackTimeoutMs,
            int stageDataAckTimeoutMs,
            CancellationToken ct)
        {
            if (_rpc == null || _ackCoordinator == null || stageData == null)
            {
                return;
            }

            _rpc.SetPhase(EStagePhase.Loading);

            Debug.Log($"[StageFlow] 집도의 동기화: Actor {surgeonActorNumber}");
            await _ackCoordinator.BroadcastAndWaitAck(
                () => _rpc.SetSurgeon(surgeonActorNumber),
                handler => _rpc.OnSurgeonAckReceived += handler,
                handler => _rpc.OnSurgeonAckReceived -= handler,
                "집도의 동기화",
                ackTimeoutMs,
                ct);

            Debug.Log("[StageFlow] 스테이지 데이터 동기화");
            string json = JsonUtility.ToJson(stageData);
            await _ackCoordinator.BroadcastAndWaitAck(
                () => _rpc.BroadcastStageData(json),
                handler => _rpc.OnStageDataAckReceived += handler,
                handler => _rpc.OnStageDataAckReceived -= handler,
                "스테이지 데이터",
                stageDataAckTimeoutMs,
                ct);

            double countdownStartTime = PhotonNetwork.Time;
            _rpc.StartCountdown(countdownStartTime, countdownSeconds);
            _rpc.SetPhase(EStagePhase.Countdown);

            Debug.Log($"[StageFlow] 시작 카운트다운: {countdownSeconds:0}초");
            await UniTask.Delay(TimeSpan.FromSeconds(countdownSeconds), cancellationToken: ct);
        }

        // ── 정리 ────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_rpc != null)
            {
                _rpc.OnStageDataReceived -= HandleStageDataReceived;
            }
        }

        // ── 내부 수신 처리 ───────────────────────────────────────────

        private void HandleStageDataReceived(StageRuntimeData stageData)
        {
            _onStageDataReceived?.Invoke(stageData);
        }
    }
}
