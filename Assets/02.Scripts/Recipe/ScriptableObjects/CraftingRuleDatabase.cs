using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR
        [ContextMenu("Generate Default Rules")]
        public void GenerateDefaultRules()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogWarning("[CraftingRuleDatabase] Default rules can only be generated from a saved asset.");
                return;
            }

            RemoveExistingSubAssets(assetPath);
            Rules = new List<CraftingRuleSO>();

            AddRule(assetPath, "Sterilized Tray", ToolType.Tray, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedTray);
            AddRule(assetPath, "Sterilized Scalpel Green", ToolType.ScalpelGreen, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScalpelGreen);
            AddRule(assetPath, "Sterilized Scalpel Gray", ToolType.ScalpelGray, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScalpelGray);
            AddRule(assetPath, "Sterilized Pincette Curved", ToolType.PincetteCurved, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedPincetteCurved);
            AddRule(assetPath, "Sterilized Pincette Straight", ToolType.PincetteStraight, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedPincetteStraight);
            AddRule(assetPath, "Sterilized Scissors Small", ToolType.ScissorsSmall, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScissorsSmall);
            AddRule(assetPath, "Sterilized Scissors Large", ToolType.ScissorsLarge, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScissorsLarge);
            AddRule(assetPath, "Sterilized Scissors Clamp", ToolType.ScissorsClamp, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScissorsClamp);
            AddRule(assetPath, "Sterilized Bone Saw", ToolType.BoneSaw, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedBoneSaw);

            AddRule(assetPath, "Anesthetic Syringe", ToolType.Syringe, ActionType.Fill, ToolType.AnestheticFluid, CraftedMaterialType.AnestheticSyringe);
            AddRule(assetPath, "Sedative Syringe", ToolType.Syringe, ActionType.Fill, ToolType.SedativeFluid, CraftedMaterialType.SedativeSyringe);

            AddRule(assetPath, "Filled Potion Cyan", ToolType.EmptyBeaker, ActionType.Fill, ToolType.PotionCyan, CraftedMaterialType.FilledPotionCyan);
            AddRule(assetPath, "Filled Potion Magenta", ToolType.EmptyBeaker, ActionType.Fill, ToolType.PotionMagenta, CraftedMaterialType.FilledPotionMagenta);
            AddRule(assetPath, "Filled Potion Yellow", ToolType.EmptyBeaker, ActionType.Fill, ToolType.PotionYellow, CraftedMaterialType.FilledPotionYellow);

            AddRule(assetPath, "Mixed Potion Blue", ToolType.PotionCyan, ActionType.MixPotion, ToolType.PotionMagenta, CraftedMaterialType.MixedPotionBlue);
            AddRule(assetPath, "Mixed Potion Red", ToolType.PotionMagenta, ActionType.MixPotion, ToolType.PotionYellow, CraftedMaterialType.MixedPotionRed);
            AddRule(assetPath, "Mixed Potion Green", ToolType.PotionCyan, ActionType.MixPotion, ToolType.PotionYellow, CraftedMaterialType.MixedPotionGreen);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void AddRule(
            string assetPath,
            string displayName,
            ToolType requiredTool,
            ActionType requiredAction,
            ToolType secondaryTool,
            CraftedMaterialType resultMaterial,
            float craftingDuration = 0f)
        {
            CraftingRuleSO rule = CreateInstance<CraftingRuleSO>();
            rule.name = displayName;
            rule.DisplayName = displayName;
            rule.RequiredTool = requiredTool | secondaryTool;
            rule.RequiredAction = requiredAction;
            rule.ResultMaterial = resultMaterial;
            rule.CraftingDuration = craftingDuration;
            rule.PrefabPath = string.Empty;

            AssetDatabase.AddObjectToAsset(rule, assetPath);
            Rules.Add(rule);
        }

        private void RemoveExistingSubAssets(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is CraftingRuleSO)
                    DestroyImmediate(asset, true);
            }
        }
#endif
    }
}
