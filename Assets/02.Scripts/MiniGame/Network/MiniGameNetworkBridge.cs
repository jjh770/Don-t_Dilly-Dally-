using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.MiniGame
{
    // 미니게임 결과를 PUN2 RPC로 다른 클라이언트에 전달한다.
    // MiniGameLauncher와 함께 같은 GameObject에 부착하여 사용.
    [RequireComponent(typeof(PhotonView))]
    public sealed class MiniGameNetworkBridge : MonoBehaviourPun
    {
        // 집도의가 미니게임을 시작했음을 다른 클라이언트에 알림 (이펙트/연출용).
        public void BroadcastMiniGameStarted(MiniGameType type)
        {
            if (!PhotonNetwork.IsConnected) return;
            photonView.RPC(nameof(RPC_NotifyMiniGameStarted), RpcTarget.Others, (int)type);
        }

        // 미니게임 결과를 다른 클라이언트에 전달.
        public void BroadcastMiniGameResult(MiniGameResult result)
        {
            if (!PhotonNetwork.IsConnected) return;
            photonView.RPC(
                nameof(RPC_NotifyMiniGameResult),
                RpcTarget.Others,
                (int)result.GameType,
                result.IsSuccess
            );
        }

        [PunRPC]
        private void RPC_NotifyMiniGameStarted(int miniGameTypeInt)
        {
            var type = (MiniGameType)miniGameTypeInt;
            Debug.Log($"[MiniGameNetwork] 다른 플레이어가 {type} 미니게임을 시작함");

            // 이펙트/연출 표시가 필요하면 여기서 처리
            // 예: 환자 위에 수술 이펙트 파티클 재생
        }

        [PunRPC]
        private void RPC_NotifyMiniGameResult(int miniGameTypeInt, bool isSuccess)
        {
            var type = (MiniGameType)miniGameTypeInt;
            Debug.Log($"[MiniGameNetwork] 미니게임 결과 수신: {type}, 성공={isSuccess}");

            // 수술 진행도 업데이트, 환자 상태 반영 등.
            // 통합 시 여기에 콜백 연결.
        }
    }
}
