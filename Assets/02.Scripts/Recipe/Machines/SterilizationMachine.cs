using UnityEngine;

namespace DontDillyDally.Data
{
    // 멸균 기계 전용 보조 컴포넌트입니다.
    // 도구 멸균과 빈 트레이 멸균을 분리해서 처리합니다.
    public class SterilizationMachine : MonoBehaviour
    {
        [SerializeField] private CraftingRuleDatabase _ruleDatabase;

        public CraftingResult TrySterilizeTool(ToolType tool, int playerId)
        {
            if (_ruleDatabase == null)
            {
                return CraftingResult.Failure(CraftingFailureReason.MissingDatabase);
            }

            if (tool == ToolType.None)
            {
                return CraftingResult.Failure(CraftingFailureReason.InvalidInput);
            }

            CraftingRuleSO rule = _ruleDatabase.FindRule(tool, ActionType.Sterilize);
            if (rule == null || rule.ResultMaterial == CraftedMaterialType.Unknown)
            {
                return CraftingResult.Failure(CraftingFailureReason.RuleNotFound);
            }

            return CraftingResult.Succeed(rule, tool, ActionType.Sterilize, playerId);
        }

        public bool CanSterilizeTray(SubmittedTray tray)
        {
            return tray != null && !tray.IsSterilizedTray && !tray.HasAnyItems();
        }

        public bool CanSterilizeTray(TrayItem trayItem)
        {
            return trayItem != null && !trayItem.IsSterilizedTray && CanSterilizeTray(trayItem.TrayData);
        }

        public bool TrySterilizeTray(SubmittedTray tray)
        {
            if (!CanSterilizeTray(tray))
            {
                return false;
            }

            tray.MarkSterilized();
            return true;
        }

        public bool TrySterilizeTray(TrayItem trayItem)
        {
            if (trayItem == null)
            {
                return false;
            }

            trayItem.EnsureTrayData();
            if (!TrySterilizeTray(trayItem.TrayData))
            {
                return false;
            }

            trayItem.SetTrayKindAndSync(TrayKind.Sterilized);
            return true;
        }
    }
}
