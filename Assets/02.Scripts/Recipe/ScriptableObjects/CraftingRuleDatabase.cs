using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DontDillyDally.Data
{
    [CreateAssetMenu(fileName = "CraftingRuleDatabase", menuName = "DontDillyDally/Crafting Rule Database")]
    public class CraftingRuleDatabase : ScriptableObject
    {
        [Header("Rules")]
        [Tooltip("All crafting rules used by the game.")]
        [FormerlySerializedAs("rules")]
        public List<CraftingRuleSO> Rules = new List<CraftingRuleSO>();

        public CraftingRuleSO FindDualRule(ToolType tool1, ToolType tool2, ActionType action)
        {
            foreach (CraftingRuleSO rule in Rules)
            {
                if (rule != null && rule.IsMatchDual(tool1, tool2, action))
                    return rule;
            }

            return null;
        }

        public CraftingRuleSO FindRule(ToolType tool, ActionType action)
        {
            foreach (CraftingRuleSO rule in Rules)
            {
                if (rule != null && rule.IsMatch(tool, action))
                    return rule;
            }

            return null;
        }

        public List<CraftingRuleSO> FindRulesForTool(ToolType tool)
        {
            List<CraftingRuleSO> results = new List<CraftingRuleSO>();
            foreach (CraftingRuleSO rule in Rules)
            {
                if (rule != null &&
                    tool != ToolType.None &&
                    (rule.RequiredTool & tool) == tool)
                {
                    results.Add(rule);
                }
            }

            return results;
        }
    }
}
