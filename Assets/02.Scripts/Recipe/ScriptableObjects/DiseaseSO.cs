using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DontDillyDally.Data
{
    // 고정 질병 데이터를 담는 ScriptableObject입니다.
    // 에디터에서 미리 정의한 질병 레시피 묶음을 저장할 때 사용합니다.
    [CreateAssetMenu(fileName = "NewDisease", menuName = "DontDillyDally/Disease Data")]
    public class DiseaseSO : ScriptableObject
    {
        public DiseaseData data;

        private void OnValidate()
        {
            if (HasMeaningfulData())
                return;

            ApplySampleData();
        }

        [ContextMenu("샘플 질병 데이터 채우기")]
        public void ApplySampleData()
        {
            data = new DiseaseData
            {
                DiseaseId = "sample_appendicitis",
                DiseaseName = "급성 충수염",
                Description = "환자당 4단계 레시피 흐름을 확인하기 위한 샘플 질병입니다.",
                FailHealthPenalty = 20f,
                Source = RecipeSource.Predefined,
                Recipes = new List<RecipeData>
                {
                    new RecipeData
                    {
                        RecipeId = "appendicitis_step_01",
                        DisplayName = "마취 투여",
                        RequiredMaterials = new List<CraftedMaterialType>
                        {
                            CraftedMaterialType.AnestheticSyringe
                        },
                        Order = 0,
                        Source = RecipeSource.Predefined,
                        RequiresSterilizedTray = true
                    },
                    new RecipeData
                    {
                        RecipeId = "appendicitis_step_02",
                        DisplayName = "절개 준비",
                        RequiredMaterials = new List<CraftedMaterialType>
                        {
                            CraftedMaterialType.SterilizedScalpelGreen,
                            CraftedMaterialType.SterilizedPincetteStraight,
                            CraftedMaterialType.GauzeBox
                        },
                        Order = 1,
                        Source = RecipeSource.Predefined,
                        RequiresSterilizedTray = true
                    },
                    new RecipeData
                    {
                        RecipeId = "appendicitis_step_03",
                        DisplayName = "염증 부위 절제",
                        RequiredMaterials = new List<CraftedMaterialType>
                        {
                            CraftedMaterialType.SterilizedScissorsSmall,
                            CraftedMaterialType.SterilizedPincetteCurved,
                            CraftedMaterialType.RedMedicine
                        },
                        Order = 2,
                        Source = RecipeSource.Predefined,
                        RequiresSterilizedTray = true
                    },
                    new RecipeData
                    {
                        RecipeId = "appendicitis_step_04",
                        DisplayName = "봉합 및 마무리",
                        RequiredMaterials = new List<CraftedMaterialType>
                        {
                            CraftedMaterialType.Bandage,
                            CraftedMaterialType.Disinfectant,
                            CraftedMaterialType.MixedPotionGreen
                        },
                        Order = 3,
                        Source = RecipeSource.Predefined,
                        RequiresSterilizedTray = true
                    }
                }
            };

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        private bool HasMeaningfulData()
        {
            return data != null &&
                   !string.IsNullOrWhiteSpace(data.DiseaseId) &&
                   data.Recipes != null &&
                   data.Recipes.Count > 0;
        }
    }
}
