using System;
using System.Collections.Generic;
using System.Linq;

namespace DontDillyDally.Data
{
    // 수술 실패 사유 (환자 체력 감소)
    public enum SurgeryFailureReason
    {
        None = 0,
        RecipeMismatch,
        MiniGameFailure,
    }

    // 게임 실패 사유 (게임 종료)
    public enum GameOverReason
    {
        None = 0,
        PatientHealthDepleted,
        TimeOut,
    }

    // 치료 판정 결과입니다.
    // UI 갱신이나 게임 진행 상태 반영에 사용합니다.
    [Serializable]
    public class SurgeryJudgeResult
    {
        public bool Success;
        public bool DiseaseCured;
        public string MatchedRecipeId;
        public float OverallProgress;
        public SurgeryFailureReason SurgeryFailure;
        public GameOverReason GameOver;
    }

    // 현재 질병의 다음 레시피를 판정하고 치료 진행도를 관리합니다.
    // 환자 체력과 제출한 트레이의 정답 여부를 함께 확인합니다.
    public class SurgeryRecipeJudge
    {
        private readonly HashSet<string> completedRecipeIds = new HashSet<string>();

        public DiseaseData CurrentDisease { get; private set; }

        public void SetDisease(DiseaseData disease)
        {
            CurrentDisease = disease;
            completedRecipeIds.Clear();
        }

        public SurgeryJudgeResult JudgeNextRecipe(
            SubmittedTray submittedTray,
            float patientHealth)
        {
            List<string> completedIds = completedRecipeIds.ToList();
            float progress = CurrentDisease?.GetOverallProgress(completedIds) ?? 0f;

            if (CurrentDisease == null)
            {
                return CreateResult(progress);
            }

            RecipeData nextRecipe = CurrentDisease.GetNextRecipe(completedIds);
            if (nextRecipe == null)
            {
                return CreateResult(
                    progress,
                    diseaseCured: CurrentDisease.IsAllRecipesCompleted(completedIds));
            }

            if (patientHealth <= 0f)
            {
                return CreateResult(progress, gameFailure: GameOverReason.PatientHealthDepleted);
            }

            if (!nextRecipe.IsSatisfiedBy(submittedTray))
            {
                return CreateResult(progress, surgeryFailure: SurgeryFailureReason.RecipeMismatch);
            }

            return CreateResult(
                progress,
                success: true,
                matchedRecipeId: nextRecipe.RecipeId);
        }

        public SurgeryJudgeResult ConfirmRecipeCompletion(string recipeId)
        {
            List<string> completedIds = completedRecipeIds.ToList();
            float progress = CurrentDisease?.GetOverallProgress(completedIds) ?? 0f;

            if (CurrentDisease == null || string.IsNullOrWhiteSpace(recipeId))
            {
                return CreateResult(progress);
            }

            completedRecipeIds.Add(recipeId);
            completedIds = completedRecipeIds.ToList();

            return CreateResult(
                CurrentDisease.GetOverallProgress(completedIds),
                success: true,
                diseaseCured: CurrentDisease.IsAllRecipesCompleted(completedIds),
                matchedRecipeId: recipeId);
        }

        private static SurgeryJudgeResult CreateResult(
            float progress,
            bool success = false,
            bool diseaseCured = false,
            string matchedRecipeId = null,
            SurgeryFailureReason surgeryFailure = SurgeryFailureReason.None,
            GameOverReason gameFailure = GameOverReason.None)
        {
            return new SurgeryJudgeResult
            {
                Success = success,
                DiseaseCured = diseaseCured,
                MatchedRecipeId = matchedRecipeId,
                OverallProgress = progress,
                SurgeryFailure = surgeryFailure,
                GameOver = gameFailure
            };
        }
    }
}
