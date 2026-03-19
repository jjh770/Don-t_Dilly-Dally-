using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    public class PotionMixingMachine : MonoBehaviour
    {
        private enum PotionFamily
        {
            None = 0,
            Raw,
            Mixed
        }

        [SerializeField] private CraftingMachine _craftingMachine;

        public bool TryResolvePotionInput(ItemObject itemObject, out CraftedMaterialType potionType, out ToolType sourceMask)
        {
            potionType = CraftedMaterialType.None;
            sourceMask = ToolType.None;

            if (itemObject is MixToolItem mixToolItem)
            {
                return TryResolveMixToolPotion(mixToolItem, out potionType, out sourceMask);
            }

            if (itemObject is BasicMaterialItem basicMaterialItem)
            {
                return TryResolveBasicPotion(basicMaterialItem, out potionType, out sourceMask);
            }

            return false;
        }

        public bool CanInsertPotion(IReadOnlyList<CraftedMaterialType> loadedPotionTypes, CraftedMaterialType candidatePotionType)
        {
            if (!IsSupportedInputPotion(candidatePotionType) || candidatePotionType == CraftedMaterialType.MixedPotionBlack)
            {
                return false;
            }

            if (loadedPotionTypes == null || loadedPotionTypes.Count == 0)
            {
                return true;
            }

            PotionFamily firstFamily = GetPotionFamily(loadedPotionTypes[0]);
            PotionFamily candidateFamily = GetPotionFamily(candidatePotionType);
            if (firstFamily == PotionFamily.None || candidateFamily == PotionFamily.None || firstFamily != candidateFamily)
            {
                return false;
            }

            for (int i = 0; i < loadedPotionTypes.Count; i++)
            {
                if (loadedPotionTypes[i] == candidatePotionType)
                {
                    return false;
                }
            }

            if (firstFamily == PotionFamily.Raw)
            {
                return loadedPotionTypes.Count < 3;
            }

            if (firstFamily == PotionFamily.Mixed)
            {
                return loadedPotionTypes.Count < 3;
            }

            return false;
        }

        public bool CanMix(IReadOnlyList<CraftedMaterialType> loadedPotionTypes)
        {
            if (_craftingMachine == null)
            {
                return false;
            }

            if (!CanBuildMixMask(loadedPotionTypes, out ToolType usedToolsMask))
            {
                return false;
            }

            return _craftingMachine.TryCraft(usedToolsMask, ActionType.MixPotion, 0).Success;
        }

        public CraftingAttemptResult TryMixPotions(IReadOnlyList<CraftedMaterialType> loadedPotionTypes, int playerId)
        {
            if (_craftingMachine == null)
            {
                return CreateFailureResult(CraftingFailureReason.MissingDatabase);
            }

            if (!CanBuildMixMask(loadedPotionTypes, out ToolType usedToolsMask))
            {
                return CreateFailureResult(CraftingFailureReason.InvalidInput);
            }

            return _craftingMachine.TryCraft(usedToolsMask, ActionType.MixPotion, playerId);
        }

        private static bool CanBuildMixMask(IReadOnlyList<CraftedMaterialType> loadedPotionTypes, out ToolType usedToolsMask)
        {
            usedToolsMask = ToolType.None;

            if (loadedPotionTypes == null || loadedPotionTypes.Count < 2)
            {
                return false;
            }

            HashSet<CraftedMaterialType> uniquePotions = new HashSet<CraftedMaterialType>();
            for (int i = 0; i < loadedPotionTypes.Count; i++)
            {
                if (!IsSupportedInputPotion(loadedPotionTypes[i]) || loadedPotionTypes[i] == CraftedMaterialType.MixedPotionBlack)
                {
                    return false;
                }

                uniquePotions.Add(loadedPotionTypes[i]);
                usedToolsMask |= ConvertPotionTypeToSourceMask(loadedPotionTypes[i]);
            }

            if (uniquePotions.Count != loadedPotionTypes.Count)
            {
                return false;
            }

            return usedToolsMask != ToolType.None;
        }

        private static bool TryResolveMixToolPotion(MixToolItem mixToolItem, out CraftedMaterialType potionType, out ToolType sourceMask)
        {
            potionType = CraftedMaterialType.None;
            sourceMask = ToolType.None;

            switch (mixToolItem.ToolType)
            {
                case ToolType.PotionCyan:
                    potionType = CraftedMaterialType.FilledPotionCyan;
                    sourceMask = ToolType.PotionCyan;
                    return true;
                case ToolType.PotionMagenta:
                    potionType = CraftedMaterialType.FilledPotionMagenta;
                    sourceMask = ToolType.PotionMagenta;
                    return true;
                case ToolType.PotionYellow:
                    potionType = CraftedMaterialType.FilledPotionYellow;
                    sourceMask = ToolType.PotionYellow;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryResolveBasicPotion(BasicMaterialItem basicMaterialItem, out CraftedMaterialType potionType, out ToolType sourceMask)
        {
            potionType = basicMaterialItem.MaterialType;
            sourceMask = ConvertPotionTypeToSourceMask(potionType);
            return sourceMask != ToolType.None && IsSupportedInputPotion(potionType);
        }

        private static ToolType ConvertPotionTypeToSourceMask(CraftedMaterialType potionType)
        {
            switch (potionType)
            {
                case CraftedMaterialType.FilledPotionCyan:
                    return ToolType.PotionCyan;
                case CraftedMaterialType.FilledPotionMagenta:
                    return ToolType.PotionMagenta;
                case CraftedMaterialType.FilledPotionYellow:
                    return ToolType.PotionYellow;
                case CraftedMaterialType.MixedPotionBlue:
                    return ToolType.PotionCyan | ToolType.PotionMagenta;
                case CraftedMaterialType.MixedPotionRed:
                    return ToolType.PotionMagenta | ToolType.PotionYellow;
                case CraftedMaterialType.MixedPotionGreen:
                    return ToolType.PotionCyan | ToolType.PotionYellow;
                default:
                    return ToolType.None;
            }
        }

        private static bool IsSupportedInputPotion(CraftedMaterialType potionType)
        {
            switch (potionType)
            {
                case CraftedMaterialType.FilledPotionCyan:
                case CraftedMaterialType.FilledPotionMagenta:
                case CraftedMaterialType.FilledPotionYellow:
                    return true;
                default:
                    return false;
            }
        }

        private static PotionFamily GetPotionFamily(CraftedMaterialType potionType)
        {
            switch (potionType)
            {
                case CraftedMaterialType.FilledPotionCyan:
                case CraftedMaterialType.FilledPotionMagenta:
                case CraftedMaterialType.FilledPotionYellow:
                    return PotionFamily.Raw;
                case CraftedMaterialType.MixedPotionBlue:
                case CraftedMaterialType.MixedPotionRed:
                case CraftedMaterialType.MixedPotionGreen:
                    return PotionFamily.Mixed;
                default:
                    return PotionFamily.None;
            }
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
