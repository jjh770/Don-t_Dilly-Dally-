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

        public CraftingRuleSO FindRule(ToolType tool, ActionType action)
        {
            foreach (CraftingRuleSO rule in Rules)
            {
                if (rule != null && rule.IsMatch(tool, action))
                {
                    return rule;
                }
            }

            return null;
        }

        public bool ContainsResult(CraftedMaterialType resultMaterial)
        {
            foreach (CraftingRuleSO rule in Rules)
            {
                if (rule != null && rule.ResultMaterial == resultMaterial)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
