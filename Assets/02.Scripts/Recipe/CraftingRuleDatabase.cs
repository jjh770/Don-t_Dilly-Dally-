using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DontDillyDally.Data
{
    // 조합 규칙 목록을 보관하는 데이터베이스입니다.
    // 기본 규칙 세트를 자동 생성해 수동 입력량을 줄일 수 있습니다.
    [CreateAssetMenu(fileName = "CraftingRuleDatabase", menuName = "DontDillyDally/Crafting Rule Database")]
    public class CraftingRuleDatabase : ScriptableObject
    {
        [Header("조합 규칙 목록")]
        [Tooltip("게임에서 사용하는 모든 조합 규칙")]
        [FormerlySerializedAs("rules")]
        public List<CraftingRuleSO> Rules = new List<CraftingRuleSO>();

        public CraftedMaterialType FindResult(ToolType tool, ActionType action)
        {
            foreach (CraftingRuleSO rule in Rules)
            {
                if (rule != null && rule.IsMatch(tool, action))
                    return rule.ResultMaterial;
            }

            return CraftedMaterialType.Unknown;
        }

        public CraftedMaterialType FindDualResult(ToolType tool1, ToolType tool2, ActionType action)
        {
            foreach (CraftingRuleSO rule in Rules)
            {
                if (rule != null && rule.IsMatchDual(tool1, tool2, action))
                    return rule.ResultMaterial;
            }

            return CraftedMaterialType.Unknown;
        }

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
                if (rule != null && rule.RequiredTool == tool)
                    results.Add(rule);
            }

            return results;
        }

#if UNITY_EDITOR
        [ContextMenu("기본 규칙 자동 생성")]
        public void GenerateDefaultRules()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogWarning("[CraftingRuleDatabase] 에셋으로 저장된 데이터베이스에서만 자동 생성을 사용할 수 있습니다.");
                return;
            }

            RemoveExistingSubAssets(assetPath);
            Rules = new List<CraftingRuleSO>();

            AddRule(assetPath, "멸균 트레이", ToolType.Tray, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedTray);
            AddRule(assetPath, "멸균 메스(초록)", ToolType.ScalpelGreen, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScalpelGreen);
            AddRule(assetPath, "멸균 메스(회색)", ToolType.ScalpelGray, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScalpelGray);
            AddRule(assetPath, "멸균 핀셋(곡선)", ToolType.PincetteCurved, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedPincetteCurved);
            AddRule(assetPath, "멸균 핀셋(직선)", ToolType.PincetteStraight, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedPincetteStraight);
            AddRule(assetPath, "멸균 가위(소형)", ToolType.ScissorsSmall, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScissorsSmall);
            AddRule(assetPath, "멸균 가위(대형)", ToolType.ScissorsLarge, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScissorsLarge);
            AddRule(assetPath, "멸균 집게 가위", ToolType.ScissorsClamp, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedScissorsClamp);
            AddRule(assetPath, "멸균 뼈톱", ToolType.BoneSaw, ActionType.Sterilize, ToolType.None, CraftedMaterialType.SterilizedBoneSaw);

            AddRule(assetPath, "마취 주사기", ToolType.Syringe, ActionType.Fill, ToolType.AnestheticFluid, CraftedMaterialType.AnestheticSyringe);
            AddRule(assetPath, "진정제 주사기", ToolType.Syringe, ActionType.Fill, ToolType.SedativeFluid, CraftedMaterialType.SedativeSyringe);

            AddRule(assetPath, "청록 물약", ToolType.EmptyBeaker, ActionType.Fill, ToolType.PotionCyan, CraftedMaterialType.FilledPotionCyan);
            AddRule(assetPath, "자주 물약", ToolType.EmptyBeaker, ActionType.Fill, ToolType.PotionMagenta, CraftedMaterialType.FilledPotionMagenta);
            AddRule(assetPath, "노랑 물약", ToolType.EmptyBeaker, ActionType.Fill, ToolType.PotionYellow, CraftedMaterialType.FilledPotionYellow);

            AddRule(assetPath, "파랑 물약", ToolType.PotionCyan, ActionType.MixPotion, ToolType.PotionMagenta, CraftedMaterialType.MixedPotionBlue);
            AddRule(assetPath, "빨강 물약", ToolType.PotionMagenta, ActionType.MixPotion, ToolType.PotionYellow, CraftedMaterialType.MixedPotionRed);
            AddRule(assetPath, "초록 물약", ToolType.PotionCyan, ActionType.MixPotion, ToolType.PotionYellow, CraftedMaterialType.MixedPotionGreen);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CraftingRuleDatabase] 기본 조합 규칙 {Rules.Count}개를 생성했습니다.");
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
            rule.RequiredTool = requiredTool;
            rule.RequiredAction = requiredAction;
            rule.SecondaryTool = secondaryTool;
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
