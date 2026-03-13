using UnityEngine;

namespace DontDillyDally.Data
{
    // 멸균 기계 전용 래퍼입니다.
    // 도구 멸균과 빈 트레이 멸균을 분리해서 처리합니다.
    public class SterilizationMachine : MonoBehaviour
    {
        [Header("연동 대상")]
        [Tooltip("도구 멸균에 사용할 조합 기계")]
        public CraftingMachine CraftingMachine;

        public CraftingAttemptResult TrySterilizeTool(ToolType tool, int playerId)
        {
            if (CraftingMachine == null)
            {
                return new CraftingAttemptResult
                {
                    Success = false,
                    FailureReason = CraftingFailureReason.MissingDatabase,
                    ResultMaterial = CraftedMaterialType.Unknown
                };
            }

            return CraftingMachine.TryCraft(tool, ActionType.Sterilize, playerId);
        }

        public bool CanSterilizeTray(SubmittedTray tray)
        {
            return tray != null && !tray.IsSterilized && !tray.HasAnyItems();
        }

        public bool TrySterilizeTray(SubmittedTray tray)
        {
            if (!CanSterilizeTray(tray))
                return false;

            tray.MarkSterilized();
            return true;
        }
    }
}
