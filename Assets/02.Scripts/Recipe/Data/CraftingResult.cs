using System;
using UnityEngine;

namespace DontDillyDally.Data
{
    public enum CraftingFailureReason
    {
        None = 0,
        MissingDatabase,
        InvalidInput,
        RuleNotFound
    }

    [Serializable]
    public class CraftingResult
    {
        public bool Success;
        public CraftedItem CraftedItem;
        public CraftedMaterialType ResultMaterial;
        public float CraftingDuration;
        public CraftingFailureReason FailureReason;

        public static CraftingResult Failure(CraftingFailureReason reason)
        {
            return new CraftingResult
            {
                Success = false,
                CraftedItem = null,
                ResultMaterial = CraftedMaterialType.Unknown,
                CraftingDuration = 0f,
                FailureReason = reason
            };
        }

        public static CraftingResult Succeed(CraftingRuleSO rule, ToolType usedToolsMask, ActionType action, int playerId)
        {
            return new CraftingResult
            {
                Success = true,
                CraftedItem = new CraftedItem
                {
                    MaterialType = rule.ResultMaterial,
                    UsedToolsMask = usedToolsMask,
                    UsedAction = action,
                    PreparedTime = Time.time,
                    PreparedByPlayerId = playerId
                },
                ResultMaterial = rule.ResultMaterial,
                CraftingDuration = rule.CraftingDuration,
                FailureReason = CraftingFailureReason.None
            };
        }
    }
}
