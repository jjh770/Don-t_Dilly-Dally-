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

        // 첫 ACK 대기 구간 안에 모든 동기화가 끝났는지 반환합니다.
        // 실패해도 게임 시작은 이어지고, 미응답 대상은 백그라운드 재동기화를 계속 시도합니다.
        public async UniTask<bool> SynchronizeStageStart(
            StageRuntimeData stageData,
            int surgeonActorNumber,
            float countdownSeconds,
            int ackTimeoutMs,
            int stageDataAckTimeoutMs,
            CancellationToken ct)
        {
            if (_rpc == null || _ackCoordinator == null || stageData == null)
            {
                return false;
            }

            _rpc.SetPhase(EStagePhase.Loading);

            Debug.Log($"[StageFlow] 집도의 동기화 시작: Actor {surgeonActorNumber}");
            bool surgeonAckCompleted = await _ackCoordinator.BroadcastAndWaitAck(
                () => _rpc.SetSurgeon(surgeonActorNumber),
                handler => _rpc.OnSurgeonAckReceived += handler,
                handler => _rpc.OnSurgeonAckReceived -= handler,
                "집도의 동기화",
                ackTimeoutMs,
                ct);

            if (!surgeonAckCompleted)
            {
                Debug.LogWarning("[StageFlow] 집도의 ACK가 아직 모두 오지 않았지만 게임 시작은 계속 진행합니다.");
            }

            Debug.Log("[StageFlow] 스테이지 데이터 동기화 시작");
            string json = JsonUtility.ToJson(stageData);
            bool stageDataAckCompleted = await _ackCoordinator.BroadcastAndWaitAck(
                () => _rpc.BroadcastStageData(json),
                handler => _rpc.OnStageDataAckReceived += handler,
                handler => _rpc.OnStageDataAckReceived -= handler,
                "스테이지 데이터",
                stageDataAckTimeoutMs,
                ct);

            if (!stageDataAckCompleted)
            {
                Debug.LogWarning("[StageFlow] 스테이지 데이터 ACK가 아직 모두 오지 않았지만 게임 시작은 계속 진행합니다.");
            }

            double countdownStartTime = PhotonNetwork.Time;
            _rpc.StartCountdown(countdownStartTime, countdownSeconds);
            _rpc.SetPhase(EStagePhase.Countdown);

            Debug.Log($"[StageFlow] 시작 카운트다운 {countdownSeconds:0}초");
            await UniTask.Delay(TimeSpan.FromSeconds(countdownSeconds), cancellationToken: ct);
            return surgeonAckCompleted && stageDataAckCompleted;
        }

        public void Dispose()
        {
            if (_rpc != null)
            {
                _rpc.OnStageDataReceived -= HandleStageDataReceived;
            }
        }

        private void HandleStageDataReceived(StageRuntimeData stageData)
        {
            _onStageDataReceived?.Invoke(stageData);
        }
    }
}
