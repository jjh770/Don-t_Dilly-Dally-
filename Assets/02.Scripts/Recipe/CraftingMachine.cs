using System;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 조합 실패 사유입니다.
    public enum CraftingFailureReason
    {
        None = 0,
        MissingDatabase,
        InvalidInput,
        RuleNotFound
    }

    // 조합 시도 결과입니다.
    [Serializable]
    public class CraftingAttemptResult
    {
        public bool Success;
        public CraftedItem CraftedItem;
        public CraftedMaterialType ResultMaterial;
        public float CraftingDuration;
        public CraftingFailureReason FailureReason;
    }

    // 도구와 행동을 받아 조합 결과물을 생성하는 기계입니다.
    // 상호작용 팀은 이 컴포넌트의 메서드만 호출하면 됩니다.
    public class CraftingMachine : MonoBehaviour
    {
        [Header("조합 설정")]
        [Tooltip("이 기계가 사용하는 조합 규칙 데이터베이스")]
        public CraftingRuleDatabase RuleDatabase;

        public CraftingAttemptResult TryCraft(
            ToolType tool,
            ActionType action,
            int playerId)
        {
            if (RuleDatabase == null)
                return CreateFailureResult(CraftingFailureReason.MissingDatabase);

            if (tool == ToolType.None || action == ActionType.None)
                return CreateFailureResult(CraftingFailureReason.InvalidInput);

            CraftingRuleSO rule = RuleDatabase.FindRule(tool, action);
            if (rule == null || rule.ResultMaterial == CraftedMaterialType.Unknown)
                return CreateFailureResult(CraftingFailureReason.RuleNotFound);

            CraftedItem craftedItem = CreateCraftedItem(
                rule.ResultMaterial,
                tool,
                action,
                ToolType.None,
                playerId);

            return new CraftingAttemptResult
            {
                Success = true,
                CraftedItem = craftedItem,
                ResultMaterial = rule.ResultMaterial,
                CraftingDuration = rule.CraftingDuration,
                FailureReason = CraftingFailureReason.None
            };
        }

        public CraftingAttemptResult TryCraft(
            ToolType primaryTool,
            ToolType secondaryTool,
            ActionType action,
            int playerId)
        {
            if (RuleDatabase == null)
                return CreateFailureResult(CraftingFailureReason.MissingDatabase);

            if (primaryTool == ToolType.None || secondaryTool == ToolType.None || action == ActionType.None)
                return CreateFailureResult(CraftingFailureReason.InvalidInput);

            CraftedMaterialType resultMaterial = RuleDatabase.FindDualResult(primaryTool, secondaryTool, action);
            if (resultMaterial == CraftedMaterialType.Unknown)
                return CreateFailureResult(CraftingFailureReason.RuleNotFound);

            CraftedItem craftedItem = CreateCraftedItem(
                resultMaterial,
                primaryTool,
                action,
                secondaryTool,
                playerId);

            CraftingRuleSO rule = RuleDatabase.FindDualRule(primaryTool, secondaryTool, action);

            return new CraftingAttemptResult
            {
                Success = true,
                CraftedItem = craftedItem,
                ResultMaterial = resultMaterial,
                CraftingDuration = rule != null ? rule.CraftingDuration : 0f,
                FailureReason = CraftingFailureReason.None
            };
        }

        private static CraftedItem CreateCraftedItem(
            CraftedMaterialType resultMaterial,
            ToolType tool,
            ActionType action,
            ToolType secondaryTool,
            int playerId)
        {
            return new CraftedItem
            {
                MaterialType = resultMaterial,
                UsedTool = tool,
                UsedAction = action,
                SecondaryTool = secondaryTool,
                PreparedTime = Time.time,
                PreparedByPlayerId = playerId
            };
        }

        private static CraftingAttemptResult CreateFailureResult(CraftingFailureReason reason)
        {
            return new CraftingAttemptResult
            {
                Success = false,
                CraftedItem = null,
                ResultMaterial = CraftedMaterialType.Unknown,
                CraftingDuration = 0f,
                FailureReason = reason
            };
        }
    }
}
