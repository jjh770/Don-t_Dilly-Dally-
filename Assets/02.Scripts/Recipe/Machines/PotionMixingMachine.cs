using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    public class PotionMixingMachine : MonoBehaviour
    {
        private const ToolType SupportedPotionMask = ToolType.PotionCyan | ToolType.PotionMagenta | ToolType.PotionYellow;

        [SerializeField] private CraftingRuleDatabase _ruleDatabase;

        public bool TryResolvePotionInput(ItemObject itemObject, out ToolType potionToolType)
        {
            potionToolType = ToolType.None;

            if (itemObject is MixToolItem mixToolItem)
            {
                potionToolType = mixToolItem.ToolType;
                return IsSupportedPotion(potionToolType);
            }

            return false;
        }

        public bool CanInsertPotion(IReadOnlyList<ToolType> loadedPotions, ToolType candidate)
        {
            if (!IsSupportedPotion(candidate))
            {
                return false;
            }

            if (loadedPotions == null || loadedPotions.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < loadedPotions.Count; i++)
            {
                if (loadedPotions[i] == candidate)
                {
                    return false;
                }
            }

            return loadedPotions.Count < 3;
        }

        public bool CanMix(IReadOnlyList<ToolType> loadedPotions)
        {
            if (_ruleDatabase == null)
            {
                return false;
            }

            if (!TryBuildMixMask(loadedPotions, out ToolType usedToolsMask))
            {
                return false;
            }

            CraftingRuleSO rule = _ruleDatabase.FindRule(usedToolsMask, ActionType.MixPotion);
            return rule != null && rule.ResultMaterial != CraftedMaterialType.Unknown;
        }

        public CraftingResult TryMixPotions(IReadOnlyList<ToolType> loadedPotions, int playerId)
        {
            if (_ruleDatabase == null)
            {
                return CraftingResult.Failure(CraftingFailureReason.MissingDatabase);
            }

            if (!TryBuildMixMask(loadedPotions, out ToolType usedToolsMask))
            {
                return CraftingResult.Failure(CraftingFailureReason.InvalidInput);
            }

            CraftingRuleSO rule = _ruleDatabase.FindRule(usedToolsMask, ActionType.MixPotion);
            if (rule == null || rule.ResultMaterial == CraftedMaterialType.Unknown)
            {
                return CraftingResult.Failure(CraftingFailureReason.RuleNotFound);
            }

            return CraftingResult.Succeed(rule, usedToolsMask, ActionType.MixPotion, playerId);
        }

        private static bool TryBuildMixMask(IReadOnlyList<ToolType> loadedPotions, out ToolType usedToolsMask)
        {
            usedToolsMask = ToolType.None;

            if (loadedPotions == null || loadedPotions.Count < 2)
            {
                return false;
            }

            for (int i = 0; i < loadedPotions.Count; i++)
            {
                ToolType potion = loadedPotions[i];

                if (!IsSupportedPotion(potion))
                {
                    return false;
                }

                if ((usedToolsMask & potion) != ToolType.None)
                {
                    return false;
                }

                usedToolsMask |= potion;
            }

            return usedToolsMask != ToolType.None;
        }

        private static bool IsSupportedPotion(ToolType toolType)
        {
            return toolType != ToolType.None && (toolType & SupportedPotionMask) == toolType;
        }
    }
}
