using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using System;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 한 환자의 레시피 제출, 판정, 실패 시 긴급 이벤트 연계를 전담합니다.
    public sealed class StageRecipeProgressCoordinator
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly TraySubmissionHandler _trayHandler;
        private readonly SurgeryRecipeJudge _recipeJudge;
        private readonly EmergencyEventPolicy _emergencyPolicy;
        private readonly StageEmergencyCoordinator _emergencyCoordinator;
        private readonly IStageRecipeProgressHost _host;

        public StageRecipeProgressCoordinator(
            StageFlowRpcHandler rpc,
            TraySubmissionHandler trayHandler,
            EmergencyEventPolicy emergencyPolicy,
            StageEmergencyCoordinator emergencyCoordinator,
            IStageRecipeProgressHost host)
        {
            _rpc = rpc;
            _trayHandler = trayHandler;
            _emergencyPolicy = emergencyPolicy;
            _emergencyCoordinator = emergencyCoordinator;
            _host = host;
            _recipeJudge = new SurgeryRecipeJudge();
        }

        // ── 레시피 진행 상태 ─────────────────────────────────────────

        public bool IsWaitingForSubmission { get; private set; }

        public void PrepareForDisease(DiseaseData disease)
        {
            _recipeJudge.SetDisease(disease);
        }

        // ── 레시피 루프 ──────────────────────────────────────────────

        public async UniTask RunRecipeLoop(DiseaseData disease, UniTask<bool> forceSuccessTask, CancellationToken ct)
        {
            int recipeIndex = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                _rpc?.SetRecipeIndex(recipeIndex);
                Debug.Log($"[StageFlow]     레시피 {recipeIndex + 1} 대기 중... (트레이 제출 대기)");

                IsWaitingForSubmission = true;
                UniTask<SubmittedTray> waitForTrayTask = _trayHandler.WaitForSubmission(ct);

                var (completedTaskIndex, tray, _) = await UniTask.WhenAny(waitForTrayTask, forceSuccessTask);
                IsWaitingForSubmission = false;

                if (completedTaskIndex == 1)
                {
                    Debug.Log("[StageFlow]     디버그 요청으로 현재 환자를 성공 처리합니다.");
                    return;
                }

                Debug.Log("[StageFlow]     트레이 제출됨 → 판정 중...");
                SurgeryJudgeResult result = _recipeJudge.JudgeNextRecipe(tray, _host != null ? _host.PatientHealth : 0f);

                if (result.Success)
                {
                    Debug.Log($"[StageFlow]     ✓ 레시피 {recipeIndex + 1} 일치! (ID: {result.MatchedRecipeId})");

                    bool shouldAdvanceRecipe = _host != null &&
                        await _host.RunRecipeMiniGame(ct);

                    if (shouldAdvanceRecipe)
                    {
                        SurgeryJudgeResult completionResult = _recipeJudge.ConfirmRecipeCompletion(result.MatchedRecipeId);
                        Debug.Log($"[StageFlow]     레시피 {recipeIndex + 1} 완료 확정! (ID: {completionResult.MatchedRecipeId}) | 질병완치={completionResult.DiseaseCured}");

                        if (completionResult.DiseaseCured)
                        {
                            Debug.Log("[StageFlow]     ★ 질병 완치!");
                            return;
                        }

                        recipeIndex++;
                    }

                    continue;
                }

                if (result.SurgeryFailure != SurgeryFailureReason.RecipeMismatch)
                {
                    continue;
                }

                float recipeFailPenalty = _host?.StageData?.Settings?.PatientSettings?.RecipeFailPenalty ?? disease.FailHealthPenalty;
                float newHealth = _host != null ? _host.ApplyDamage(recipeFailPenalty) : 0f;
                Debug.Log($"[StageFlow]     ✗ 레시피 실패! 체력 -{recipeFailPenalty} → 현재 체력: {newHealth}");
                EventManager.Instance?.OnSurgeryFail(result.SurgeryFailure);

                if (_host != null && _host.IsGameOver)
                {
                    Debug.Log("[StageFlow]     !! 환자 사망 → 게임 오버");
                    return;
                }

                if (_emergencyPolicy == null || !_emergencyPolicy.ShouldTriggerOnRecipeFail(_host?.StageData))
                {
                    continue;
                }

                Debug.Log("[StageFlow]     ⚡ 긴급 이벤트 발동!");

                if (_emergencyCoordinator != null &&
                    _emergencyCoordinator.TryStartEmergencyEvent(
                        EmergencyTriggerSource.RecipeFail,
                        _host != null ? _host.CurrentPhase : EStagePhase.None,
                        _host != null && _host.IsGameOver,
                        IsWaitingForSubmission))
                {
                    await _emergencyCoordinator.WaitForResult(ct);
                }
            }
        }
    }
}
