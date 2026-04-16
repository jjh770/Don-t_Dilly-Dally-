using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    // DiseaseDTO를 DiseaseData로 변환하는 정적 유틸리티입니다.
    // JSON에서 파싱한 DTO를 게임 시스템이 사용하는 DiseaseData로 매핑합니다.
    public static class DiseaseConverter
    {
        public static DiseaseData Convert(DiseaseDTO dto)
        {
            if (dto == null)
            {
                Debug.LogWarning("[DiseaseConverter] DTO가 null입니다.");
                return null;
            }

            var disease = new DiseaseData
            {
                StageId = dto.stageId,
                DiseaseId = dto.diseaseId,
                DiseaseName = dto.diseaseName,
                Description = dto.description,
                PatientName = dto.patientName,
                Backstory = dto.backstory,
                Source = RecipeSource.Predefined,
                Recipes = new List<RecipeData>()
            };

            if (dto.recipes == null || dto.recipes.Count == 0)
            {
                Debug.LogWarning($"[DiseaseConverter] '{dto.diseaseId}' 레시피가 비어 있습니다.");
                return null;
            }

            for (int i = 0; i < dto.recipes.Count; i++)
            {
                RecipeData recipe = ConvertRecipe(dto.recipes[i], dto.diseaseId, i);
                if (recipe == null)
                    return null;

                disease.Recipes.Add(recipe);
            }

            if (!disease.Validate())
            {
                Debug.LogWarning($"[DiseaseConverter] '{dto.diseaseId}' 검증 실패.");
                return null;
            }

            return disease;
        }

        public static List<DiseaseData> ConvertAll(FallbackDiseaseCollection collection)
        {
            var results = new List<DiseaseData>();

            if (collection?.diseases == null)
            {
                Debug.LogWarning("[DiseaseConverter] 컬렉션이 비어 있습니다.");
                return results;
            }

            for (int i = 0; i < collection.diseases.Count; i++)
            {
                DiseaseData disease = Convert(collection.diseases[i]);
                if (disease != null)
                    results.Add(disease);
            }

            return results;
        }

        private static RecipeData ConvertRecipe(RecipeStepDTO stepDto, string diseaseId, int index)
        {
            if (stepDto == null)
            {
                Debug.LogWarning($"[DiseaseConverter] '{diseaseId}' 인덱스 {index} 레시피 DTO가 null입니다.");
                return null;
            }

            var recipe = new RecipeData
            {
                RecipeId = stepDto.recipeId,
                DisplayName = stepDto.displayName,
                Order = stepDto.order,
                Source = RecipeSource.Predefined,
                RequiresSterilizedTray = stepDto.requiresSterilizedTray,
                RequiredMaterials = new List<CraftedMaterialType>()
            };

            if (stepDto.requiredMaterials == null)
            {
                Debug.LogWarning($"[DiseaseConverter] '{stepDto.recipeId}' 재료 목록이 null입니다.");
                return null;
            }

            for (int j = 0; j < stepDto.requiredMaterials.Length; j++)
            {
                CraftedMaterialType? material = ParseMaterial(stepDto.requiredMaterials[j]);
                if (material == null)
                {
                    Debug.LogWarning(
                        $"[DiseaseConverter] '{stepDto.recipeId}'에서 알 수 없는 재료: {stepDto.requiredMaterials[j]}");
                    return null;
                }

                recipe.RequiredMaterials.Add(material.Value);
            }

            return recipe;
        }

        private static CraftedMaterialType? ParseMaterial(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                return null;

            if (Enum.TryParse<CraftedMaterialType>(materialName, false, out var result))
            {
                if (result == CraftedMaterialType.None || result == CraftedMaterialType.Unknown)
                    return null;

                return result;
            }

            return null;
        }
    }
}
