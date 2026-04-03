using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using DontDillyDally.MiniGame;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // ?덉떆???뺣떟 ??誘몃땲寃뚯엫???ㅽ뻾 寃곌낵瑜??섏옄 ?곹깭? 湲닿툒 ?대깽???먮쫫??諛섏쁺?⑸땲??
    public sealed class StageMiniGameResolutionCoordinator
    {
        private readonly IStageMiniGameResolutionHost _host;

        public StageMiniGameResolutionCoordinator(IStageMiniGameResolutionHost host)
        {
            _host = host;
        }

        // ?? 誘몃땲寃뚯엫 寃곌낵 ?꾩쿂由??????????????????????????????????????

        public async UniTask<bool> RunRecipeMiniGame(CancellationToken ct)
        {
            MiniGameType type = MiniGameTypeExtensions.GetRandom();
            int surgeonActorNumber = GetMiniGameTargetActorNumber();
            Debug.Log($"[StageFlow]     ?덉떆??誘몃땲寃뚯엫 ?쒖옉: {type} | 吏묐룄??Actor {surgeonActorNumber}");

            bool success = _host?.MiniGameCoordinator != null &&
                await _host.MiniGameCoordinator.RunRecipeMiniGame(surgeonActorNumber, type, ct);
            Debug.Log($"[StageFlow]     ?덉떆??誘몃땲寃뚯엫 寃곌낵: {(success ? "?깃났" : "?ㅽ뙣")}");

            if (success)
            {
                float recoveredHealth = _host != null ? _host.ApplyHeal(_host.MiniGameSuccessHeal) : 0f;
                Debug.Log($"[StageFlow]     誘몃땲寃뚯엫 ?깃났! 泥대젰 +{_host?.MiniGameSuccessHeal ?? 0f} | ?꾩옱 泥대젰: {recoveredHealth}");
                EventManager.Instance?.OnSurgerySuccess();
                return true;
            }

            float newHealth = _host != null ? _host.ApplyDamage(_host.MiniGameFailPenalty) : 0f;
            Debug.Log($"[StageFlow]     誘몃땲寃뚯엫 ?ㅽ뙣! 泥대젰 -{_host?.MiniGameFailPenalty ?? 0f} | ?꾩옱 泥대젰: {newHealth}");
            EventManager.Instance?.OnSurgeryFail(SurgeryFailureReason.MiniGameFailure);

            if (_host != null && _host.IsGameOver)
            {
                Debug.Log("[StageFlow]     ?섏옄 ?щ쭩?쇰줈 寃뚯엫 ?ㅻ쾭 泥섎━");
                return false;
            }

            if (_host?.EmergencyPolicy != null && _host.EmergencyPolicy.ShouldTriggerOnMiniGameFail(_host.StageData))
            {
                Debug.Log("[StageFlow]     誘몃땲寃뚯엫 ?ㅽ뙣濡?湲닿툒 ?대깽?몃? ?쒖옉?⑸땲??");

                if (_host.EmergencyCoordinator != null &&
                    _host.EmergencyCoordinator.TryStartEmergencyEvent(
                        EmergencyTriggerSource.MiniGameFail,
                        _host.CurrentPhase,
                        _host.IsGameOver,
                        _host.IsWaitingForRecipeSubmission))
                {
                    await _host.EmergencyCoordinator.WaitForResult(ct);
                }
            }

            return false;
        }

        // ?? ?대? 蹂댁“ 硫붿꽌???????????????????????????????????????????

        private int GetMiniGameTargetActorNumber()
        {
            if (_host != null && _host.SurgeonActorNumber > 0)
            {
                return _host.SurgeonActorNumber;
            }

            if (Photon.Pun.PhotonNetwork.LocalPlayer != null)
            {
                return Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
            }

            return -1;
        }
    }
}
