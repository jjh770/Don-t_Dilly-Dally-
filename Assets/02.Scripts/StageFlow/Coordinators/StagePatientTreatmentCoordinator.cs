using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using Photon.Realtime;
using System;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 환자 전환과 환자별 치료 루프를 전담합니다.
    public sealed class StagePatientTreatmentCoordinator
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly StageTimer _timer;
        private readonly EmergencyEventPolicy _emergencyPolicy;
        private readonly IStagePatientTreatmentHost _host;
        private readonly StageRecipeProgressCoordinator _recipeProgressCoordinator;

        private UniTaskCompletionSource<bool> _forcePatientSuccessTcs;

        public StagePatientTreatmentCoordinator(
            StageFlowRpcHandler rpc,
            StageTimer timer,
            EmergencyEventPolicy emergencyPolicy,
            IStagePatientTreatmentHost host,
            StageRecipeProgressCoordinator recipeProgressCoordinator)
        {
            _rpc = rpc;
            _timer = timer;
            _emergencyPolicy = emergencyPolicy;
            _host = host;
            _recipeProgressCoordinator = recipeProgressCoordinator;
        }

        // ── 환자 치료 상태 ───────────────────────────────────────────

        public bool HasActivePatientLoop => _forcePatientSuccessTcs != null;

        // ── 환자 치료 루프 ───────────────────────────────────────────

        public async UniTask RunGameLoop(float patientTransitionDelaySec, CancellationToken ct)
        {
            StageRuntimeData stageData = _host?.StageData;
            if (stageData == null || _rpc == null || _timer == null)
            {
                return;
            }

            _timer.Set(stageData.Settings.TotalTimeLimitSec);
            _timer.Resume();
            _host?.SyncTimerState();
            Debug.Log($"[StageFlow]   타이머 시작: {stageData.Settings.TotalTimeLimitSec}초 | 환자 {stageData.Patients.Count}명 치료 시작");

            for (int i = 0; i < stageData.Patients.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                _rpc.SetPatientIndex(i);

                if (i > 0)
                {
                    Debug.Log("[StageFlow]   환자 전환 중... (타이머 일시정지)");
                    _host?.PauseDrain();
                    _timer.Pause();
                    _host?.SyncTimerState();
                    _rpc.SetPhase(EStagePhase.PatientTransition);
                    await UniTask.Delay(TimeSpan.FromSeconds(patientTransitionDelaySec), cancellationToken: ct);
                    _rpc.SetPhase(EStagePhase.Playing);
                    _timer.Resume();
                    _host?.ResumeDrain(_host.CurrentPhase);
                    _host?.SyncTimerState();
                    Debug.Log("[StageFlow]   환자 전환 완료 (타이머 재개)");
                }

                await RunPatientLoop(i, stageData, ct);
            }
        }

        public bool TryForceCompleteCurrentPatient()
        {
            if (_forcePatientSuccessTcs == null)
            {
                return false;
            }

            _forcePatientSuccessTcs.TrySetResult(true);
            return true;
        }

        // ── 내부 환자 단위 처리 ─────────────────────────────────────

        private async UniTask RunPatientLoop(int patientIndex, StageRuntimeData stageData, CancellationToken ct)
        {
            DiseaseData disease = stageData.Patients[patientIndex];
            Debug.Log($"[StageFlow] ── 환자 {patientIndex + 1}/{stageData.Patients.Count} 시작 | 병명: {disease.DiseaseName} | 레시피: {disease.Recipes?.Count ?? 0}단계 | 체력: {stageData.Settings.PatientSettings.InitialPatientHealth}");

            CommentaryController.Instance?.SetCurrentPatientIndex(patientIndex);
            EventManager.Instance?.OnNewPatientAppeared(disease.PatientName, disease.DiseaseName);

            _recipeProgressCoordinator?.PrepareForDisease(disease);
            _host?.Initialize(
                stageData.Settings.PatientSettings.InitialPatientHealth,
                stageData.Settings.PatientSettings.PatientHealthDrainPerSecond,
                _host != null ? _host.CurrentPhase : EStagePhase.None);
            _emergencyPolicy?.ResetTimer();
            _forcePatientSuccessTcs = new UniTaskCompletionSource<bool>();

            try
            {
                UniTask<bool> forceSuccessTask = _forcePatientSuccessTcs.Task;
                await _recipeProgressCoordinator.RunRecipeLoop(disease, forceSuccessTask, ct);
            }
            finally
            {
                _forcePatientSuccessTcs = null;
            }

            float currentHealth = _host != null ? _host.CurrentHealth : 0f;
            Debug.Log($"[StageFlow] ── 환자 {patientIndex + 1}/{stageData.Patients.Count} 치료 완료! | 병명: {disease.DiseaseName} | 남은 체력: {currentHealth}");
            StageFlowManager.Instance?.PerformanceTracker.Record(GetSurgeonPlayer(), disease, EPerformanceEventType.PatientSaved);
            stageData.SavedCount += 1;
        }

        private Player GetSurgeonPlayer()
        {
            int actorNumber = StageFlowManager.Instance != null
                ? StageFlowManager.Instance.SurgeonActorNumber.Value
                : -1;

            if (PhotonServerManager.Instance != null &&
                PhotonServerManager.Instance.TryGetPlayerByActorNumber(actorNumber, out Player player))
            {
                return player;
            }

            return null;
        }
    }
}
