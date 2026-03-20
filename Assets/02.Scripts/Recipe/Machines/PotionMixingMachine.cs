using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    public class PotionMixingMachine : MonoBehaviour
    {
        private const ToolType SupportedPotionMask = ToolType.PotionCyan | ToolType.PotionMagenta | ToolType.PotionYellow;

        [SerializeField] private CraftingMachine _craftingMachine;

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
            if (_craftingMachine == null)
            {
                return false;
            }

            if (!TryBuildMixMask(loadedPotions, out ToolType usedToolsMask))
            {
                return false;
            }

            return _craftingMachine.TryCraft(usedToolsMask, ActionType.MixPotion, 0).Success;
        }

        public CraftingAttemptResult TryMixPotions(IReadOnlyList<ToolType> loadedPotions, int playerId)
        {
            if (_craftingMachine == null)
            {
                return CreateFailureResult(CraftingFailureReason.MissingDatabase);
            }

            if (!TryBuildMixMask(loadedPotions, out ToolType usedToolsMask))
            {
                return CreateFailureResult(CraftingFailureReason.InvalidInput);
            }

            return _craftingMachine.TryCraft(usedToolsMask, ActionType.MixPotion, playerId);
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
