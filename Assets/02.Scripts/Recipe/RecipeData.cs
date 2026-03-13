using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DontDillyDally.Data
{
    // 긴급 이벤트에서 사용하는 진단 스캔 종류입니다.
    // 메인 치료 레시피 판정에는 사용하지 않습니다.
    public enum DiagnosisScanType
    {
        None = 0,
        Ultrasound,
        XRay,
        ECG
    }

    // 치료 레시피 데이터입니다.
    // 하나의 레시피는 환자에게 제출해야 하는 재료 조건을 가집니다.
    [Serializable]
    public class RecipeData
    {
        public const int FirstRecipeMaterialCount = 1;
        public const int MinTreatmentMaterials = 2;
        public const int MaxTreatmentMaterials = 4;

        [Header("레시피 정보")]
        [Tooltip("레시피를 식별하는 고유 ID")]
        [FormerlySerializedAs("recipeId")]
        public string RecipeId;

        [Tooltip("UI에 표시할 레시피 이름")]
        [FormerlySerializedAs("displayName")]
        public string DisplayName;

        [Tooltip("레시피 완성에 필요한 재료 목록")]
        [FormerlySerializedAs("requiredMaterials")]
        public List<CraftedMaterialType> RequiredMaterials;

        [Header("진행 정보")]
        [Tooltip("질병 치료 흐름 안에서의 순서")]
        [FormerlySerializedAs("order")]
        public int Order;

        [Tooltip("고정 데이터인지 AI 생성 데이터인지 구분")]
        [FormerlySerializedAs("source")]
        public RecipeSource Source;

        [Header("제출 조건")]
        [Tooltip("이 레시피가 멸균 트레이 제출을 요구하는지 여부")]
        public bool RequiresSterilizedTray = true;

        // 이전 데이터와 호환되도록 레거시 트레이 재료는 비교 대상에서 제외합니다.
        public List<CraftedMaterialType> GetNormalizedRequiredMaterials()
        {
            if (RequiredMaterials == null)
                return new List<CraftedMaterialType>();

            return RequiredMaterials
                .Where(material => material != CraftedMaterialType.SterilizedTray)
                .ToList();
        }

        public bool IsMatch(List<CraftedMaterialType> materials)
        {
            List<CraftedMaterialType> normalizedRequiredMaterials = GetNormalizedRequiredMaterials();
            if (materials == null || materials.Count != normalizedRequiredMaterials.Count)
                return false;

            List<CraftedMaterialType> sortedRequired = normalizedRequiredMaterials.OrderBy(material => material).ToList();
            List<CraftedMaterialType> sortedInput = materials.OrderBy(material => material).ToList();

            return sortedRequired.SequenceEqual(sortedInput);
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(RecipeId))
            {
                Debug.LogWarning("[RecipeData] 레시피 ID가 비어 있습니다.");
                return false;
            }

            List<CraftedMaterialType> normalizedRequiredMaterials = GetNormalizedRequiredMaterials();
            if (normalizedRequiredMaterials.Count == 0)
            {
                Debug.LogWarning($"[RecipeData] '{RecipeId}'에 재료가 비어 있습니다.");
                return false;
            }

            if (normalizedRequiredMaterials.Contains(CraftedMaterialType.Unknown))
            {
                Debug.LogWarning($"[RecipeData] '{RecipeId}'에 Unknown 재료가 포함되어 있습니다.");
                return false;
            }

            if (normalizedRequiredMaterials.Contains(CraftedMaterialType.None))
            {
                Debug.LogWarning($"[RecipeData] '{RecipeId}'에 None 재료가 포함되어 있습니다.");
                return false;
            }

            return true;
        }

        // 모든 병의 첫 레시피는 멸균 트레이 위 마취약 1개여야 합니다.
        public bool ValidateAsFirstRecipe()
        {
            if (!Validate())
                return false;

            List<CraftedMaterialType> normalizedRequiredMaterials = GetNormalizedRequiredMaterials();
            if (normalizedRequiredMaterials.Count != FirstRecipeMaterialCount ||
                normalizedRequiredMaterials[0] != CraftedMaterialType.AnestheticSyringe)
            {
                Debug.LogWarning($"[RecipeData] '{RecipeId}' 첫 레시피는 마취약 1개만 필요해야 합니다.");
                return false;
            }

            if (!RequiresSterilizedTray)
            {
                Debug.LogWarning($"[RecipeData] '{RecipeId}' 첫 레시피는 멸균 트레이 제출이 필수입니다.");
                return false;
            }

            return true;
        }

        // 첫 레시피 이후의 일반 치료 레시피를 검증합니다.
        public bool ValidateAsTreatmentRecipe()
        {
            if (!Validate())
                return false;

            int normalizedMaterialCount = GetNormalizedRequiredMaterials().Count;
            if (normalizedMaterialCount < MinTreatmentMaterials ||
                normalizedMaterialCount > MaxTreatmentMaterials)
            {
                Debug.LogWarning(
                    $"[RecipeData] '{RecipeId}' 일반 치료 레시피 재료 수는 {MinTreatmentMaterials}~{MaxTreatmentMaterials}개여야 합니다.");
                return false;
            }

            return true;
        }

        public float GetProgress(List<CraftedMaterialType> currentMaterials)
        {
            List<CraftedMaterialType> normalizedRequiredMaterials = GetNormalizedRequiredMaterials();
            if (normalizedRequiredMaterials.Count == 0 || currentMaterials == null)
                return 0f;

            int matched = 0;
            List<CraftedMaterialType> remaining = new List<CraftedMaterialType>(normalizedRequiredMaterials);

            foreach (CraftedMaterialType material in currentMaterials)
            {
                if (remaining.Remove(material))
                    matched++;
            }

            return (float)matched / normalizedRequiredMaterials.Count;
        }
    }

    public enum RecipeSource
    {
        Predefined,
        AIGenerated
    }
}
