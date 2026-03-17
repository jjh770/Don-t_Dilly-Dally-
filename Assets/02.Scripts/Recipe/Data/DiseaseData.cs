using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DontDillyDally.Data
{
    // 질병 데이터입니다.
    // 하나의 질병은 4~5개의 치료 레시피로 구성되고 전체 진행도 계산에 사용됩니다.
    [Serializable]
    public class DiseaseData
    {
        public const int MinRecipeCount = 4;
        public const int MaxRecipeCount = 5;

        [Header("질병 정보")]
        [Tooltip("질병을 식별하는 고유 ID")]
        [FormerlySerializedAs("diseaseId")]
        public string DiseaseId;

        [Tooltip("질병 이름")]
        [FormerlySerializedAs("diseaseName")]
        public string DiseaseName;

        [Tooltip("질병 설명")]
        [FormerlySerializedAs("description")]
        public string Description;

        [Header("치료 흐름")]
        [Tooltip("이 질병을 치료하기 위해 필요한 레시피 목록")]
        [FormerlySerializedAs("recipes")]
        public List<RecipeData> Recipes;

        [Header("실패 설정")]
        [Tooltip("치료 실패 시 적용할 환자 체력 감소량")]
        [FormerlySerializedAs("failHealthPenalty")]
        public float FailHealthPenalty;

        [Header("메타 정보")]
        [FormerlySerializedAs("source")]
        public RecipeSource Source;

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(DiseaseId) || string.IsNullOrWhiteSpace(DiseaseName))
            {
                Debug.LogWarning("[DiseaseData] 질병 ID 또는 이름이 비어 있습니다.");
                return false;
            }

            if (Recipes == null || Recipes.Count < MinRecipeCount || Recipes.Count > MaxRecipeCount)
            {
                Debug.LogWarning(
                    $"[DiseaseData] '{DiseaseName}'의 레시피 수가 {Recipes?.Count ?? 0}개입니다. {MinRecipeCount}~{MaxRecipeCount}개여야 합니다.");
                return false;
            }

            List<RecipeData> orderedRecipes = Recipes.OrderBy(recipe => recipe.Order).ToList();

            if (!orderedRecipes[0].ValidateAsFirstRecipe())
                return false;

            if (!HasUniqueRecipeIds())
            {
                Debug.LogWarning($"[DiseaseData] '{DiseaseName}'에 중복된 레시피 ID가 있습니다.");
                return false;
            }

            if (!HasUniqueRecipeOrder())
            {
                Debug.LogWarning($"[DiseaseData] '{DiseaseName}'에 중복된 레시피 순서가 있습니다.");
                return false;
            }

            foreach (RecipeData recipe in orderedRecipes.Skip(1))
            {
                if (!recipe.ValidateAsTreatmentRecipe())
                    return false;
            }

            return true;
        }

        public float GetOverallProgress(List<string> completedRecipeIds)
        {
            if (Recipes == null || Recipes.Count == 0 || completedRecipeIds == null)
                return 0f;

            int completed = Recipes.Count(recipe => completedRecipeIds.Contains(recipe.RecipeId));
            return (float)completed / Recipes.Count;
        }

        public bool IsAllRecipesCompleted(List<string> completedRecipeIds)
        {
            return Recipes != null &&
                   completedRecipeIds != null &&
                   Recipes.All(recipe => completedRecipeIds.Contains(recipe.RecipeId));
        }

        public bool ShouldGrantReward(List<string> completedRecipeIds)
        {
            return IsAllRecipesCompleted(completedRecipeIds);
        }

        public RecipeData GetNextRecipe(List<string> completedRecipeIds)
        {
            List<string> completed = completedRecipeIds ?? new List<string>();

            return Recipes?
                .OrderBy(recipe => recipe.Order)
                .FirstOrDefault(recipe => !completed.Contains(recipe.RecipeId));
        }

        private bool HasUniqueRecipeIds()
        {
            return Recipes
                .Where(recipe => !string.IsNullOrWhiteSpace(recipe.RecipeId))
                .Select(recipe => recipe.RecipeId)
                .Distinct()
                .Count() == Recipes.Count;
        }

        private bool HasUniqueRecipeOrder()
        {
            return Recipes
                .Select(recipe => recipe.Order)
                .Distinct()
                .Count() == Recipes.Count;
        }
    }
}
