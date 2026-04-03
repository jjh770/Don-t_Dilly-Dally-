using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using Photon.Pun;
using System;
using System.Threading;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 긴급 이벤트의 시작, 진행, 종료 동기화를 전담합니다.
    public sealed class StageEmergencyCoordinator : IDisposable
    {
        private readonly StageFlowRpcHandler _rpc;
        private readonly Func<CancellationToken> _flowCancellationTokenProvider;

        private UniTaskCompletionSource<EmergencyResumeResult> _emergencyResultTcs;
        private CancellationTokenSource _diagnosisOperateCts;

        public StageEmergencyCoordinator(
            StageFlowRpcHandler rpc,
            Func<CancellationToken> flowCancellationTokenProvider)
        {
            _rpc = rpc;
            _flowCancellationTokenProvider = flowCancellationTokenProvider;
            Controller = new EmergencyEventController();

            if (_rpc != null)
            {
                _rpc.OnEmergencyMaterialSubmittedReceived += HandleEmergencyMaterialSubmitted;
                _rpc.OnEmergencyDiagnosisOperateReceived += HandleEmergencyDiagnosisOperateReceived;
                _rpc.OnEmergencyStartedReceived += HandleEmergencyStartedReceived;
                _rpc.OnEmergencyEndedReceived += HandleEmergencyEndedReceived;
            }
        }

        public EmergencyEventController Controller { get; }
        public bool IsActive => Controller != null && Controller.IsActive;
        public bool IsDiagnosisOperating => Controller != null && Controller.IsDiagnosisOperating;
        public float DiagnosisOperationRemainingTime => Controller?.DiagnosisOperationRemainingTime ?? 0f;
        public EmergencyEventKind CurrentKind => Controller?.CurrentKind ?? EmergencyEventKind.None;
        public float RemainingTime => Controller?.RemainingTime ?? 0f;
        public CraftedMaterialType CurrentTrayTarget => Controller?.CurrentTrayTarget ?? CraftedMaterialType.None;
        public DiagnosisScanType CurrentDiagnosisTarget => Controller?.CurrentDiagnosisTarget ?? DiagnosisScanType.None;

        public bool CanSubmitTrayToPatient()
        {
            return Controller == null || Controller.CanSubmitTrayToPatient();
        }

        // 현재 상태에서 긴급 이벤트를 시작할 수 있으면 시작과 브로드캐스트를 함께 처리합니다.
        public bool TryStartEmergencyEvent(
            EmergencyTriggerSource triggerSource,
            EStagePhase currentPhase,
            bool isGameOver,
            bool isWaitingForRecipeSubmission)
        {
            if (Controller == null || _rpc == null)
            {
                return false;
            }

            if (!Controller.CanBegin(currentPhase, isGameOver, isWaitingForRecipeSubmission, triggerSource))
            {
                return false;
            }

            EmergencyEventKind kind = SelectEmergencyKind();
            if (!Controller.TryBegin(kind, triggerSource))
            {
                return false;
            }

            _emergencyResultTcs?.TrySetCanceled();
            _emergencyResultTcs = new UniTaskCompletionSource<EmergencyResumeResult>();

            EventManager.Instance?.OnPatientCritical("긴급 처치가 필요합니다.");
            _rpc.BroadcastEmergency(
                kind,
                triggerSource,
                Controller.CurrentTrayTarget,
                Controller.CurrentDiagnosisTarget);

            Debug.Log($"[StageFlow] 긴급 이벤트 시작: {kind} | 원인: {triggerSource}");
            return true;
        }

        // StageFlow가 긴급 이벤트 종료 결과만 기다릴 수 있도록 결과 대기를 제공합니다.
        public async UniTask<EmergencyResumeResult> WaitForResult(CancellationToken ct)
        {
            _emergencyResultTcs ??= new UniTaskCompletionSource<EmergencyResumeResult>();

            using (ct.Register(() => _emergencyResultTcs.TrySetCanceled()))
            {
                return await _emergencyResultTcs.Task;
            }
        }

        // 트레이 기반 긴급 이벤트 제출 요청을 마스터 판정 흐름으로 전달합니다.
        public bool RequestEmergencyMaterialSubmission(CraftedMaterialType materialType, int itemViewId = -1)
        {
            if (!IsActive || CurrentKind != EmergencyEventKind.Tray || materialType == CraftedMaterialType.None)
            {
                return false;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                HandleEmergencyMaterialSubmitted(materialType, itemViewId, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
            }
            else
            {
                _rpc?.SubmitEmergencyMaterial(materialType, itemViewId);
            }

            return true;
        }

        // 진단 장비 기반 긴급 이벤트 조작 요청을 마스터 판정 흐름으로 전달합니다.
        public bool RequestDiagnosisOperation(DiagnosisScanType diagnosisType)
        {
            if (!IsActive || CurrentKind != EmergencyEventKind.Diagnosis || diagnosisType == DiagnosisScanType.None)
            {
                return false;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                HandleEmergencyDiagnosisOperateReceived(diagnosisType, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
            }
            else
            {
                Controller?.TryBeginDiagnosisOperationLocally(diagnosisType);
                _rpc?.SubmitEmergencyDiagnosisOperate(diagnosisType);
            }

            return true;
        }

        // 제한 시간이 끝난 긴급 이벤트를 실패 처리합니다.
        public bool TryHandleTimeout()
        {
            if (!IsActive || Controller == null || !Controller.HasTimedOut())
            {
                return false;
            }

            Debug.Log("[StageFlow] 긴급 이벤트 제한 시간이 초과되었습니다.");
            CompleteEmergencyFailure();
            return true;
        }

        public void Dispose()
        {
            _emergencyResultTcs?.TrySetCanceled();
            _emergencyResultTcs = null;

            CancelDiagnosisOperateTask();

            if (_rpc != null)
            {
                _rpc.OnEmergencyMaterialSubmittedReceived -= HandleEmergencyMaterialSubmitted;
                _rpc.OnEmergencyDiagnosisOperateReceived -= HandleEmergencyDiagnosisOperateReceived;
                _rpc.OnEmergencyStartedReceived -= HandleEmergencyStartedReceived;
                _rpc.OnEmergencyEndedReceived -= HandleEmergencyEndedReceived;
            }
        }

        private static EmergencyEventKind SelectEmergencyKind()
        {
            return UnityEngine.Random.value < 0.5f
                ? EmergencyEventKind.Tray
                : EmergencyEventKind.Diagnosis;
        }

        private void CompleteEmergencySuccess()
        {
            if (!IsActive || Controller == null)
            {
                return;
            }

            CancelDiagnosisOperateTask();
            EmergencyResumeResult result = Controller.ResolveSuccess();
            _rpc?.BroadcastEmergencyEnd();
            _emergencyResultTcs?.TrySetResult(result);
            _emergencyResultTcs = null;
        }

        private void CompleteEmergencyFailure()
        {
            if (!IsActive || Controller == null)
            {
                return;
            }

            CancelDiagnosisOperateTask();
            EmergencyResumeResult result = Controller.ResolveFailure();
            _rpc?.BroadcastEmergencyEnd();
            _emergencyResultTcs?.TrySetResult(result);
            _emergencyResultTcs = null;
        }

        private void HandleEmergencyStartedReceived(
            EmergencyEventKind kind,
            EmergencyTriggerSource triggerSource,
            CraftedMaterialType trayTarget,
            DiagnosisScanType diagnosisTarget)
        {
            if (PhotonNetwork.IsMasterClient || Controller == null)
            {
                return;
            }

            Controller.SyncBegin(kind, triggerSource, trayTarget, diagnosisTarget);
        }

        private void HandleEmergencyEndedReceived()
        {
            if (PhotonNetwork.IsMasterClient || Controller == null)
            {
                return;
            }

            Controller.End();
        }

        private void HandleEmergencyMaterialSubmitted(CraftedMaterialType materialType, int itemViewId, int submitterActorNumber)
        {
            if (!PhotonNetwork.IsMasterClient || Controller == null || !IsActive)
            {
                return;
            }

            if (CurrentKind != EmergencyEventKind.Tray)
            {
                return;
            }

            if (Controller.EvaluateEmergencyMaterialSubmission(materialType))
            {
                Debug.Log($"[StageFlow] 긴급 재료 제출 성공: Actor {submitterActorNumber}");
                CompleteEmergencySuccess();
                return;
            }

            Debug.Log($"[StageFlow] 긴급 재료 제출 실패: Actor {submitterActorNumber}");
            CompleteEmergencyFailure();
        }

        private void HandleEmergencyDiagnosisOperateReceived(DiagnosisScanType diagnosisType, int submitterActorNumber)
        {
            if (!PhotonNetwork.IsMasterClient || Controller == null || !IsActive)
            {
                return;
            }

            if (CurrentKind != EmergencyEventKind.Diagnosis)
            {
                return;
            }

            EmergencyDiagnosisOperationResult result = Controller.TryBeginDiagnosisOperation(diagnosisType);
            switch (result)
            {
                case EmergencyDiagnosisOperationResult.Started:
                    Debug.Log($"[StageFlow] 진단 긴급 이벤트 시작: {diagnosisType} | Actor {submitterActorNumber}");
                    RunDiagnosisEmergencySuccess().Forget();
                    break;

                case EmergencyDiagnosisOperationResult.Failed:
                    Debug.Log($"[StageFlow] 진단 긴급 이벤트 실패: {diagnosisType} | Actor {submitterActorNumber}");
                    CompleteEmergencyFailure();
                    break;
            }
        }

        // 진단 긴급 이벤트는 일정 시간 장비 작동 유지 후 성공 처리합니다.
        private async UniTaskVoid RunDiagnosisEmergencySuccess()
        {
            CancelDiagnosisOperateTask();

            CancellationToken flowToken = CancellationToken.None;
            if (_flowCancellationTokenProvider != null)
            {
                flowToken = _flowCancellationTokenProvider();
            }

            _diagnosisOperateCts = CancellationTokenSource.CreateLinkedTokenSource(flowToken);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(5f), cancellationToken: _diagnosisOperateCts.Token);

                if (IsActive &&
                    CurrentKind == EmergencyEventKind.Diagnosis &&
                    IsDiagnosisOperating)
                {
                    CompleteEmergencySuccess();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelDiagnosisOperateTask()
        {
            if (_diagnosisOperateCts == null)
            {
                return;
            }

            _diagnosisOperateCts.Cancel();
            _diagnosisOperateCts.Dispose();
            _diagnosisOperateCts = null;
        }
    }
}

