using Cysharp.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // RPC 전파 뒤 다른 클라이언트의 ACK를 기다리는 공용 동기화 헬퍼입니다.
    public sealed class StageRpcAckCoordinator
    {
        private readonly int _retryDelayMs;

        public StageRpcAckCoordinator(int retryDelayMs)
        {
            _retryDelayMs = retryDelayMs;
        }

        public async UniTask BroadcastAndWaitAck(
            Action broadcast,
            Action<Action<int>> subscribe,
            Action<Action<int>> unsubscribe,
            string label,
            int timeoutMs,
            CancellationToken ct)
        {
            int otherPlayerCount = PhotonNetwork.PlayerList.Length - 1;
            if (otherPlayerCount <= 0)
            {
                Debug.Log($"[StageFlow]   {label}: 다른 플레이어 없음, ACK 생략");
                broadcast();
                return;
            }

            HashSet<int> pendingActors = new HashSet<int>();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!player.IsLocal)
                {
                    pendingActors.Add(player.ActorNumber);
                }
            }

            UniTaskCompletionSource allAckTcs = new UniTaskCompletionSource();

            void OnAck(int actorNumber)
            {
                pendingActors.Remove(actorNumber);
                Debug.Log($"[StageFlow]   {label} ACK: Actor {actorNumber} (남은 {pendingActors.Count}명)");
                if (pendingActors.Count == 0)
                {
                    allAckTcs.TrySetResult();
                }
            }

            subscribe(OnAck);

            try
            {
                broadcast();

                bool completed = await UniTask.WhenAny(
                    allAckTcs.Task,
                    UniTask.Delay(timeoutMs, cancellationToken: ct)) == 0;

                if (completed)
                {
                    Debug.Log($"[StageFlow]   {label}: 모든 클라이언트 확인 완료");
                }
                else
                {
                    Debug.LogWarning($"[StageFlow]   {label}: ACK 타임아웃 ({pendingActors.Count}명 미응답) - 재전송. 미응답 Actor: {string.Join(", ", pendingActors)}");
                    broadcast();
                    await UniTask.Delay(_retryDelayMs, cancellationToken: ct);
                }
            }
            finally
            {
                unsubscribe(OnAck);
            }
        }
    }
}
