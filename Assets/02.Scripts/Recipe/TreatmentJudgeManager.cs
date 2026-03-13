using System;
using System.Collections.Generic;
using System.Linq;

namespace DontDillyDally.Data
{
    // 치료 판정 실패 사유입니다.
    public enum TreatmentFailureReason
    {
        None = 0,
        NoActiveDisease,
        NoRemainingRecipe,
        PatientHealthDepleted,
        RecipeMismatch
    }

    // 치료 판정 결과입니다.
    // UI 갱신이나 게임 진행 상태 반영에 사용합니다.
    [Serializable]
    public class TreatmentJudgeResult
    {
        public bool Success;
        public bool DiseaseCured;
        public string CompletedRecipeId;
        public float OverallProgress;
        public TreatmentFailureReason FailureReason;
    }

    // 현재 질병의 다음 레시피를 판정하고 치료 진행도를 관리합니다.
    // 환자 체력과 제출한 트레이의 정답 여부를 함께 확인합니다.
    public class TreatmentJudgeManager
    {
        private readonly HashSet<string> completedRecipeIds = new HashSet<string>();

        public DiseaseData CurrentDisease { get; private set; }

        public IReadOnlyCollection<string> CompletedRecipeIds => completedRecipeIds;

        public void SetDisease(DiseaseData disease)
        {
            CurrentDisease = disease;
            completedRecipeIds.Clear();
        }

        public TreatmentJudgeResult JudgeNextRecipe(
            SubmittedTray submittedTray,
            float patientHealth)
        {
            if (CurrentDisease == null)
            {
                return CreateFailureResult(TreatmentFailureReason.NoActiveDisease);
            }

            List<string> completedIds = completedRecipeIds.ToList();
            RecipeData nextRecipe = CurrentDisease.GetNextRecipe(completedIds);
            if (nextRecipe == null)
            {
                return CreateFailureResult(
                    TreatmentFailureReason.NoRemainingRecipe,
                    CurrentDisease.GetOverallProgress(completedIds),
                    CurrentDisease.IsAllRecipesCompleted(completedIds));
            }

            if (patientHealth <= 0f)
            {
                return CreateFailureResult(
                    TreatmentFailureReason.PatientHealthDepleted,
                    CurrentDisease.GetOverallProgress(completedIds));
            }

            if (!CraftedItem.MatchesRecipe(submittedTray, nextRecipe))
            {
                return CreateFailureResult(
                    TreatmentFailureReason.RecipeMismatch,
                    CurrentDisease.GetOverallProgress(completedIds));
            }

            completedRecipeIds.Add(nextRecipe.RecipeId);
            completedIds = completedRecipeIds.ToList();

            return new TreatmentJudgeResult
            {
                Success = true,
                DiseaseCured = CurrentDisease.IsAllRecipesCompleted(completedIds),
                CompletedRecipeId = nextRecipe.RecipeId,
                OverallProgress = CurrentDisease.GetOverallProgress(completedIds),
                FailureReason = TreatmentFailureReason.None
            };
        }

        public void ResetProgress()
        {
            completedRecipeIds.Clear();
        }

        private static TreatmentJudgeResult CreateFailureResult(
            TreatmentFailureReason reason,
            float progress = 0f,
            bool diseaseCured = false)
        {
            return new TreatmentJudgeResult
            {
                Success = false,
                DiseaseCured = diseaseCured,
                CompletedRecipeId = null,
                OverallProgress = progress,
                FailureReason = reason
            };
        }
    }
}
