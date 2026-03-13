using System;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 실제 플레이 중 조합되거나 준비된 제출 아이템 데이터입니다.
    // 어떤 도구와 행동으로 준비되었는지 추적할 수 있습니다.
    [Serializable]
    public class CraftedItem
    {
        public CraftedMaterialType MaterialType;
        public ToolType UsedTool;
        public ActionType UsedAction;
        public ToolType SecondaryTool;
        public float PreparedTime;
        public int PreparedByPlayerId;

        public bool MatchesMaterial(CraftedMaterialType required)
        {
            return MaterialType == required;
        }

        public static bool MatchesRecipe(SubmittedTray submittedTray, RecipeData recipe)
        {
            if (submittedTray == null || recipe == null)
                return false;

            if (recipe.RequiresSterilizedTray && !submittedTray.IsSterilized)
                return false;

            return recipe.IsMatch(submittedTray.GetContainedMaterialTypes());
        }

        // 가공 없이 바로 제출 가능한 기본 재료를 제출 아이템으로 감쌉니다.
        public static CraftedItem CreateBasicMaterial(
            CraftedMaterialType materialType,
            int playerId = 0)
        {
            if (!IsBasicMaterial(materialType))
            {
                Debug.LogWarning($"[CraftedItem] '{materialType}'는 기본 재료 생성 대상이 아닙니다.");
                return null;
            }

            return new CraftedItem
            {
                MaterialType = materialType,
                UsedTool = ToolType.None,
                UsedAction = ActionType.None,
                SecondaryTool = ToolType.None,
                PreparedTime = Time.time,
                PreparedByPlayerId = playerId
            };
        }

        public static bool IsBasicMaterial(CraftedMaterialType materialType)
        {
            switch (materialType)
            {
                case CraftedMaterialType.Bandage:
                case CraftedMaterialType.Disinfectant:
                case CraftedMaterialType.Stethoscope:
                case CraftedMaterialType.AmbuBag:
                case CraftedMaterialType.GauzeBox:
                case CraftedMaterialType.RedMedicine:
                case CraftedMaterialType.OrganLiver:
                case CraftedMaterialType.OrganStomach:
                case CraftedMaterialType.OrganLung:
                case CraftedMaterialType.OrganIntestine:
                case CraftedMaterialType.Defibrillator:
                case CraftedMaterialType.BloodPack:
                    return true;

                default:
                    return false;
            }
        }

        public string GetDisplayName()
        {
            switch (MaterialType)
            {
                case CraftedMaterialType.SterilizedTray:
                    return "멸균 트레이";
                case CraftedMaterialType.SterilizedScalpelGreen:
                    return "멸균 메스(초록)";
                case CraftedMaterialType.SterilizedScalpelGray:
                    return "멸균 메스(회색)";
                case CraftedMaterialType.SterilizedPincetteCurved:
                    return "멸균 핀셋(곡선)";
                case CraftedMaterialType.SterilizedPincetteStraight:
                    return "멸균 핀셋(직선)";
                case CraftedMaterialType.SterilizedScissorsSmall:
                    return "멸균 가위(소형)";
                case CraftedMaterialType.SterilizedScissorsLarge:
                    return "멸균 가위(대형)";
                case CraftedMaterialType.SterilizedScissorsClamp:
                    return "멸균 지혈 가위";
                case CraftedMaterialType.SterilizedBoneSaw:
                    return "멸균 뼈톱";
                case CraftedMaterialType.AnestheticSyringe:
                    return "마취 주사기";
                case CraftedMaterialType.SedativeSyringe:
                    return "진정제 주사기";
                case CraftedMaterialType.FilledPotionCyan:
                    return "청록 물약";
                case CraftedMaterialType.FilledPotionMagenta:
                    return "자주 물약";
                case CraftedMaterialType.FilledPotionYellow:
                    return "노랑 물약";
                case CraftedMaterialType.MixedPotionBlue:
                    return "파랑 물약";
                case CraftedMaterialType.MixedPotionRed:
                    return "빨강 물약";
                case CraftedMaterialType.MixedPotionGreen:
                    return "초록 물약";
                case CraftedMaterialType.Bandage:
                    return "붕대";
                case CraftedMaterialType.Disinfectant:
                    return "소독약";
                case CraftedMaterialType.Stethoscope:
                    return "청진기";
                case CraftedMaterialType.AmbuBag:
                    return "앰부백";
                case CraftedMaterialType.GauzeBox:
                    return "거즈";
                case CraftedMaterialType.RedMedicine:
                    return "빨강 약";
                case CraftedMaterialType.OrganLiver:
                    return "간";
                case CraftedMaterialType.OrganStomach:
                    return "위";
                case CraftedMaterialType.OrganLung:
                    return "폐";
                case CraftedMaterialType.OrganIntestine:
                    return "장";
                case CraftedMaterialType.Defibrillator:
                    return "제세동기";
                case CraftedMaterialType.BloodPack:
                    return "혈액팩";
                case CraftedMaterialType.Unknown:
                default:
                    return "알 수 없는 결과물";
            }
        }
    }
}
