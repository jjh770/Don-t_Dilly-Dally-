using System;
using System.Collections.Generic;
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
        [Tooltip("이 질병이 속한 스테이지 ID")]
        public string StageId;

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

        [Header("환자 정보")]
        [Tooltip("환자 이름")]
        [FormerlySerializedAs("patientName")]
        public string PatientName;

        [Tooltip("환자 배경 이야기")]
        [FormerlySerializedAs("backstory")]
        [TextArea(2, 4)]
        public string Backstory;

        [Header("게임 설정")]
        [Tooltip("난이도 (1~5)")]
        [FormerlySerializedAs("difficulty")]
        [Range(1, 5)]
        public int Difficulty = 1;

        [Tooltip("제한 시간 (초). 0이면 제한 없음")]
        [FormerlySerializedAs("timeLimitSec")]
        public float TimeLimitSec;

        [Tooltip("권장 플레이어 수 (2~4)")]
        [FormerlySerializedAs("recommendedPlayers")]
        [Range(2, 4)]
        public int RecommendedPlayers = 2;

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

            // 첫 번째 레시피 찾기 (Order가 가장 낮은 것)
            RecipeData firstRecipe = FindRecipeByMinOrder();
            if (firstRecipe == null || !firstRecipe.ValidateAsFirstRecipe())
                return false;

            // 나머지 레시피 검증
            int firstOrder = firstRecipe.Order;
            for (int i = 0; i < Recipes.Count; i++)
            {
                if (Recipes[i].Order != firstOrder && !Recipes[i].ValidateAsTreatmentRecipe())
                    return false;
            }

            return true;
        }

        public float GetOverallProgress(List<string> completedRecipeIds)
        {
            if (Recipes == null || Recipes.Count == 0 || completedRecipeIds == null)
                return 0f;

            int completedCount = 0;
            for (int i = 0; i < Recipes.Count; i++)
            {
                for (int j = 0; j < completedRecipeIds.Count; j++)
                {
                    if (Recipes[i].RecipeId == completedRecipeIds[j])
                    {
                        completedCount++;
                        break;
                    }
                }
            }

            return (float)completedCount / Recipes.Count;
        }

        public bool IsAllRecipesCompleted(List<string> completedRecipeIds)
        {
            if (Recipes == null || completedRecipeIds == null)
                return false;

            for (int i = 0; i < Recipes.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < completedRecipeIds.Count; j++)
                {
                    if (Recipes[i].RecipeId == completedRecipeIds[j])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        public bool ShouldGrantReward(List<string> completedRecipeIds)
        {
            return IsAllRecipesCompleted(completedRecipeIds);
        }

        public RecipeData GetNextRecipe(List<string> completedRecipeIds)
        {
            RecipeData nextRecipe = null;
            int minOrder = int.MaxValue;

            for (int i = 0; i < Recipes.Count; i++)
            {
                RecipeData recipe = Recipes[i];
                if (recipe.Order >= minOrder)
                    continue;

                bool isCompleted = false;
                if (completedRecipeIds != null)
                {
                    for (int j = 0; j < completedRecipeIds.Count; j++)
                    {
                        if (recipe.RecipeId == completedRecipeIds[j])
                        {
                            isCompleted = true;
                            break;
                        }
                    }
                }

                if (!isCompleted)
                {
                    minOrder = recipe.Order;
                    nextRecipe = recipe;
                }
            }

            return nextRecipe;
        }

        private RecipeData FindRecipeByMinOrder()
        {
            if (Recipes == null || Recipes.Count == 0)
                return null;

            RecipeData minRecipe = Recipes[0];
            for (int i = 1; i < Recipes.Count; i++)
            {
                if (Recipes[i].Order < minRecipe.Order)
                    minRecipe = Recipes[i];
            }

            return minRecipe;
        }

        private bool HasUniqueRecipeIds()
        {
            var idSet = new HashSet<string>(Recipes.Count);
            for (int i = 0; i < Recipes.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(Recipes[i].RecipeId) && !idSet.Add(Recipes[i].RecipeId))
                    return false;
            }

            return idSet.Count == Recipes.Count;
        }

        private bool HasUniqueRecipeOrder()
        {
            var orderSet = new HashSet<int>(Recipes.Count);
            for (int i = 0; i < Recipes.Count; i++)
            {
                if (!orderSet.Add(Recipes[i].Order))
                    return false;
            }

            return true;
        }
    }
}
