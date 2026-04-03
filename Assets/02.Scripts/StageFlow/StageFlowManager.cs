using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using DontDillyDally.MiniGame;
using Photon.Pun;
using System;
using System.Threading;
using UniRx;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public enum EStageRole
    {
        None = 0,
        Surgeon,
        Assistant
    }

    public class StageFlowManager : PunSingleton<StageFlowManager>,
        IStageFlowState,
        IStageFlowCommands,
        IStagePatientFlow,
        IStageMiniGameRunner,
        IStageOutcomeHost,
        IStageRecipeProgressHost,
        IStagePatientTreatmentHost
    {
        // ── 타이밍 상수 ─────────────────────────────────────────────
        private const int ACK_TIMEOUT_MS = 5000;
        private const int ACK_RETRY_DELAY_MS = 2000;
        private const int STAGE_DATA_ACK_TIMEOUT_MS = 10000;
        private const int GAME_OVER_ACK_TOTAL_TIMEOUT_MS = 12000;
        private const float STAGE_START_COUNTDOWN_SEC = 3f;
        private const float STAGE_CLEAR_DELAY_SEC = 5f;
        private const float PATIENT_TRANSITION_DELAY_SEC = 2f;
        private const float RETURN_TO_WAITING_ROOM_DELAY_SEC = 5f;

        // ── 위임 컴포넌트 ────────────────────────────────────────────
        [Header("핸들러")]
        [SerializeField] private StageFlowRpcHandler _rpc;
        [SerializeField] private StageTimer _timer;
        [SerializeField] private EmergencyEventPolicy _emergencyPolicy;

        [Header("미니게임 참조")]
        [SerializeField] private MiniGameLauncher _miniGameLauncher;

        [Header("미니게임")]
        [SerializeField] private float _miniGameFailPenalty = 10f;
        [SerializeField] private float _miniGameSuccessHeal = 5f;

        // ── 외부 구독용 (RpcHandler에 위임) ──────────────────────────
        public IReadOnlyReactiveProperty<EStagePhase> CurrentPhase => _rpc.CurrentPhase;
        public IReadOnlyReactiveProperty<float> PatientHealth => _rpc.PatientHealth;
        public IReadOnlyReactiveProperty<float> StageTimer => _rpc.StageTimer;
        public IReadOnlyReactiveProperty<int> CurrentPatientIndex => _rpc.CurrentPatientIndex;
        public IReadOnlyReactiveProperty<int> CurrentRecipeIndex => _rpc.CurrentRecipeIndex;
        public IReadOnlyReactiveProperty<int> SurgeonActorNumber => _rpc.SurgeonActorNumber;
        public IReadOnlyReactiveProperty<double> CountdownStartTime => _rpc.CountdownStartTime;
        public IReadOnlyReactiveProperty<float> CountdownDuration => _rpc.CountdownDuration;
        public StageRuntimeData CurrentStageData => _stageData;
        public float LocalRemainingTime => _timer != null ? _timer.RemainingTime : 0f;
        public TraySubmissionHandler TrayHandler => _trayHandler;
        public bool CanSubmitRecipeTray => _trayHandler != null && _trayHandler.CanSubmit;
        public EStageRole LocalRole => GetLocalRole();
        public bool IsLocalSurgeon => LocalRole == EStageRole.Surgeon;
        public bool IsLocalAssistant => LocalRole == EStageRole.Assistant;
        public bool IsEmergencyActive => _emergencyCoordinator != null && _emergencyCoordinator.IsActive;
        public bool IsEmergencyDiagnosisOperating => _emergencyCoordinator != null && _emergencyCoordinator.IsDiagnosisOperating;
        public float EmergencyDiagnosisOperationRemainingTime => _emergencyCoordinator?.DiagnosisOperationRemainingTime ?? 0f;
        public EmergencyEventKind CurrentEmergencyKind => _emergencyCoordinator?.CurrentKind ?? EmergencyEventKind.None;
        public float EmergencyRemainingTime => _emergencyCoordinator?.RemainingTime ?? 0f;
        public CraftedMaterialType CurrentEmergencyTrayTarget => _emergencyCoordinator?.CurrentTrayTarget ?? CraftedMaterialType.None;
        public DiagnosisScanType CurrentEmergencyDiagnosisTarget => _emergencyCoordinator?.CurrentDiagnosisTarget ?? DiagnosisScanType.None;
        public bool CanLocalInteractWithPatient =>
            CanSubmitRecipeTray &&
            IsLocalSurgeon &&
            (_emergencyCoordinator == null || _emergencyCoordinator.CanSubmitTrayToPatient());

        public bool ShouldShowEmergencyUi =>
            IsLocalSurgeon &&
            IsEmergencyActive &&
            (_emergencyCoordinator == null || !_emergencyCoordinator.IsDiagnosisOperating) &&
            (_rpc == null || _rpc.CurrentPhase.Value == EStagePhase.Playing);

        // ================================================================
        //  공개 조회 API
        // ================================================================

        // 로컬 플레이어의 현재 역할을 반환합니다.
        public EStageRole GetLocalRole()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            {
                return EStageRole.None;
            }

            return GetRoleForActorNumber(PhotonNetwork.LocalPlayer.ActorNumber);
        }

        // 특정 ActorNumber가 집도의인지 어시스트인지 판별합니다.
        public EStageRole GetRoleForActorNumber(int actorNumber)
        {
            if (_rpc == null || actorNumber <= 0 || _rpc.SurgeonActorNumber.Value <= 0)
            {
                return EStageRole.None;
            }

            return _rpc.SurgeonActorNumber.Value == actorNumber ? EStageRole.Surgeon : EStageRole.Assistant;
        }

        // 로컬 플레이어 역할을 UI용 문자열로 반환합니다.
        public string GetLocalRoleDisplayName()
        {
            return LocalRole switch
            {
                EStageRole.Surgeon => "집도의",
                EStageRole.Assistant => "어시스트",
                _ => "역할 미정"
            };
        }

        // 현재 환자에게 할당된 질병 데이터를 가져옵니다.
        public bool TryGetCurrentDisease(out DiseaseData disease)
        {
            disease = null;

            if (_stageData == null || _stageData.Patients == null || _rpc == null)
            {
                return false;
            }

            int patientIndex = _rpc.CurrentPatientIndex.Value;

            if (patientIndex < 0 || patientIndex >= _stageData.Patients.Count)
            {
                return false;
            }

            disease = _stageData.Patients[patientIndex];
            return disease != null;
        }

        // 시작 카운트다운 중 남은 시간을 반환합니다. (마스터 클라이언트 기준)
        public float GetRemainingCountdownTime()
        {
            if (_rpc == null)
            {
                return 0f;
            }

            float duration = Mathf.Max(0f, _rpc.CountdownDuration.Value);
            if (duration <= 0f)
            {
                return 0f;
            }

            double elapsed = PhotonNetwork.Time - _rpc.CountdownStartTime.Value;
            return Mathf.Max(0f, duration - (float)elapsed);
        }

        public string GetEmergencyObjectiveText()
        {
            if (!IsEmergencyActive)
            {
                return string.Empty;
            }

            if (CurrentEmergencyKind == EmergencyEventKind.Tray)
            {
                return $"{GetMaterialDisplayName(CurrentEmergencyTrayTarget)} 제출";
            }

            return CurrentEmergencyKind switch
            {
                EmergencyEventKind.Tray => $"멸균 트레이에 {GetMaterialDisplayName(CurrentEmergencyTrayTarget)} 제출",
                EmergencyEventKind.Diagnosis => $"{GetDiagnosisDisplayName(CurrentEmergencyDiagnosisTarget)} 기계를 환자에게 작동",
                _ => "긴급 처치 진행"
            };
        }

        private static string GetMaterialDisplayName(CraftedMaterialType materialType)
        {
            return materialType switch
            {
                CraftedMaterialType.SedativeSyringe => "Sedative Syringe",
                CraftedMaterialType.Defibrillator => "Defibrillator",
                CraftedMaterialType.BloodPack => "Blood Pack",
                _ => materialType.ToString()
            };
        }

        private static string GetDiagnosisDisplayName(DiagnosisScanType diagnosisType)
        {
            return diagnosisType switch
            {
                DiagnosisScanType.Radiograph => "X-Ray",
                _ => diagnosisType.ToString()
            };
        }

        // ================================================================
        //  코디네이터 호스트 구현
        // ================================================================

        StageRuntimeData IStageFlowState.StageData => _stageData;
        bool IStageFlowState.IsGameOver => _isGameOver;
        EStagePhase IStageFlowState.CurrentPhase => _rpc != null ? _rpc.CurrentPhase.Value : EStagePhase.None;
        float IStageFlowState.RemainingTime => _timer != null ? _timer.RemainingTime : 0f;
        float IStageFlowState.PatientHealth => _rpc != null ? _rpc.PatientHealth.Value : 0f;

        void IStageFlowCommands.MarkGameOver()
        {
            _isGameOver = true;
        }

        void IStageFlowCommands.PauseStageTimer()
        {
            _timer?.Pause();
        }

        void IStageFlowCommands.SyncTimerState()
        {
            SyncTimerState();
        }

        void IStageFlowCommands.ClearRoles()
        {
            SelectRoleManager.Instance?.ClearRoles();
        }

        void IStageFlowCommands.PublishGameOver(EGameOverReason reason)
        {
            OnGameOver?.Invoke(reason);
        }

        void IStageFlowCommands.PublishReward(StageReward reward, StageResult result)
        {
            OnStageRewardGranted?.Invoke(reward, result);
        }

        float IStagePatientFlow.CurrentHealth => _patientStatusCoordinator?.CurrentHealth ?? 0f;

        void IStagePatientFlow.Initialize(float maxHealth, float drainPerSecond, EStagePhase currentPhase)
        {
            _patientStatusCoordinator?.Initialize(maxHealth, drainPerSecond, currentPhase);
        }

        float IStagePatientFlow.ApplyDamage(float damage)
        {
            return _patientStatusCoordinator != null
                ? _patientStatusCoordinator.ApplyDamage(damage)
                : 0f;
        }

        float IStagePatientFlow.ApplyHeal(float heal)
        {
            return _patientStatusCoordinator != null
                ? _patientStatusCoordinator.ApplyHeal(heal)
                : 0f;
        }

        void IStagePatientFlow.PauseDrain()
        {
            _patientStatusCoordinator?.PauseDrain();
        }

        void IStagePatientFlow.ResumeDrain(EStagePhase currentPhase)
        {
            _patientStatusCoordinator?.ResumeDrain(currentPhase);
        }

        void IStagePatientFlow.Tick(float deltaTime)
        {
            _patientStatusCoordinator?.Tick(deltaTime);
        }

        UniTask<bool> IStageMiniGameRunner.RunRecipeMiniGame(CancellationToken ct)
        {
            return RunRecipeMiniGame(ct);
        }

        // ── 런타임 상태 ────────────────────────────────────────────────
        private StageRuntimeData _stageData;
        private bool _isGameOver;

        // ── 런타임 컨트롤러 ───────────────────────────────────────────
        private StageRpcAckCoordinator _ackCoordinator;
        private StageBootstrapCoordinator _bootstrapCoordinator;
        private StageMiniGameCoordinator _miniGameCoordinator;
        private StageEmergencyCoordinator _emergencyCoordinator;
        private StageMovementCoordinator _movementCoordinator;
        private StageOutcomeCoordinator _outcomeCoordinator;
        private StagePatientStatusCoordinator _patientStatusCoordinator;
        private StageRecipeProgressCoordinator _recipeProgressCoordinator;
        private StagePatientTreatmentCoordinator _patientTreatmentCoordinator;

        // ── 진행 보조 객체 ───────────────────────────────────────────
        private CancellationTokenSource _flowCts;
        private PatientHealthController _patientHealthController;
        private TraySubmissionHandler _trayHandler;

        // ── 캐시된 델리게이트 (구독 해제용) ─────────────────────────
        private Action _onTimerExpired;
        private Action<float> _onTimerSyncTick;

        // ── 이벤트 ──────────────────────────────────────────────────
        public event Action<EGameOverReason> OnGameOver;
        public event Action OnStageClear;
        public event Action<StageRuntimeData> OnStageDataChanged;
        public event Action<StageReward, StageResult> OnStageRewardGranted;

        // ================================================================
        //  초기화
        // ================================================================

        // StageFlow에 필요한 런타임 의존성과 이벤트를 초기화합니다.
        public void Initialize(StageRuntimeData stageData)
        {
            _rpc.ResetState();
            _stageData = stageData;
            _isGameOver = false;
            OnStageDataChanged?.Invoke(_stageData);

            // 제출 핸들러와 타이머 바인딩을 현재 스테이지 기준으로 다시 준비합니다.
            _trayHandler = new TraySubmissionHandler(_rpc, () => _isGameOver);
            _patientHealthController = new PatientHealthController();
            _onTimerExpired = () => TriggerGameOver(EGameOverReason.TimeExpired);
            _onTimerSyncTick = time => _rpc.SetTimer(time);
            _timer.OnExpired += _onTimerExpired;
            _timer.OnSyncTick += _onTimerSyncTick;

            // 새 스테이지 시작 전에 이전 런타임 객체를 먼저 정리합니다.
            _flowCts?.Cancel();
            _flowCts?.Dispose();
            _ackCoordinator = new StageRpcAckCoordinator(ACK_RETRY_DELAY_MS);
            _bootstrapCoordinator?.Dispose();
            _miniGameCoordinator?.Dispose();
            _emergencyCoordinator?.Dispose();
            _movementCoordinator?.Dispose();
            _outcomeCoordinator?.Dispose();
            _patientStatusCoordinator?.Dispose();
            _flowCts = new CancellationTokenSource();

            // StageFlowManager를 host로 삼아 각 코디네이터를 다시 구성합니다.
            _bootstrapCoordinator = new StageBootstrapCoordinator(_rpc, _ackCoordinator, HandleStageDataReceived);
            _miniGameCoordinator = new StageMiniGameCoordinator(_rpc, _miniGameLauncher, () => IsLocalSurgeon);
            _emergencyCoordinator = new StageEmergencyCoordinator(_rpc, () => _flowCts != null ? _flowCts.Token : CancellationToken.None);
            _movementCoordinator = new StageMovementCoordinator(_rpc);
            _outcomeCoordinator = new StageOutcomeCoordinator(_rpc, _ackCoordinator, this);
            _patientStatusCoordinator = new StagePatientStatusCoordinator(_patientHealthController, _rpc, TriggerGameOver);
            _recipeProgressCoordinator = new StageRecipeProgressCoordinator(_rpc, _trayHandler, _emergencyPolicy, _emergencyCoordinator, this);
            _patientTreatmentCoordinator = new StagePatientTreatmentCoordinator(_rpc, _timer, _emergencyPolicy, this, _recipeProgressCoordinator);

            _movementCoordinator.Initialize();

            if (PhotonNetwork.IsMasterClient)
            {
                StartStageFlow().Forget();
            }
        }

        // ================================================================
        //  마스터 전용 - 메인 흐름
        // ================================================================

        // 집도의/질병/음성은 StagePreloader가 컷씬 중 사전 생성 완료.
        // 여기서는 RPC 동기화 + 게임 루프만 실행합니다.
        private async UniTaskVoid StartStageFlow()
        {
            Debug.Log("[StageFlow] ========== 스테이지 플로우 시작 ==========");
            var ct = _flowCts.Token;

            try
            {
                int surgeonActor = StagePreloader.Instance.SurgeonActorNumber;
                await _bootstrapCoordinator.SynchronizeStageStart(
                    _stageData,
                    surgeonActor,
                    STAGE_START_COUNTDOWN_SEC,
                    ACK_TIMEOUT_MS,
                    STAGE_DATA_ACK_TIMEOUT_MS,
                    ct);

                Debug.Log("[StageFlow] ▶ Playing 진입 (게임 카운트 다운 시작)");
                Debug.Log("[StageFlow] ▶ Playing 시작 (게임 루프 시작)");
                _rpc.SetPhase(EStagePhase.Playing);
                await _patientTreatmentCoordinator.RunGameLoop(PATIENT_TRANSITION_DELAY_SEC, ct);
                Debug.Log("[StageFlow] ✓ Playing 완료 (모든 환자 치료 성공)");

                Debug.Log("[StageFlow] ▶ StageClear! 5초 후 대기실 복귀 가능");
                _patientStatusCoordinator?.PauseDrain();
                _timer.Pause();
                SyncTimerState();
                _rpc.SetPhase(EStagePhase.StageClear);
                OnStageClear?.Invoke();
                SelectRoleManager.Instance?.ClearRoles();

                await UniTask.Delay(TimeSpan.FromSeconds(STAGE_CLEAR_DELAY_SEC), cancellationToken: ct);

                Debug.Log("[StageFlow] 보상을 지급합니다.");
                _outcomeCoordinator?.ApplyReward();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StageFlow] 플로우 취소됨 (게임 오버)");
            }
        }

        // ── 트레이 제출 (외부 API — TraySubmissionHandler에 위임) ──

        // ================================================================
        //  공개 제출 API
        // ================================================================

        // 외부에서 제출된 트레이를 StageFlow 제출 핸들러로 전달합니다.
        public void OnTraySubmitted(SubmittedTray tray)
        {
            _trayHandler.OnTraySubmitted(tray);
        }

        // 환자 상호작용으로 만들어진 제출 요청을 검증 루프로 전달합니다.
        public bool RequestTraySubmission(SubmittedTray tray, int trayViewId = -1)
        {
            return _trayHandler.RequestSubmission(tray, trayViewId);
        }

        public bool RequestEmergencyMaterialSubmission(CraftedMaterialType materialType, int itemViewId = -1)
        {
            return _emergencyCoordinator != null &&
                   _emergencyCoordinator.RequestEmergencyMaterialSubmission(materialType, itemViewId);
        }

        public bool RequestDiagnosisOperation(DiagnosisScanType diagnosisType)
        {
            return _emergencyCoordinator != null &&
                   _emergencyCoordinator.RequestDiagnosisOperation(diagnosisType);
        }

        [ContextMenu("Debug/Force Complete Current Patient")]
        public void ForceCompleteCurrentPatient()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[StageFlow] 마스터 클라이언트만 현재 환자를 강제 성공 처리할 수 있습니다.");
                return;
            }

            if (_isGameOver || _stageData == null)
            {
                Debug.LogWarning("[StageFlow] 현재 상태에서는 환자 강제 성공 처리를 실행할 수 없습니다.");
                return;
            }

            if (_rpc == null || _rpc.CurrentPhase.Value != EStagePhase.Playing)
            {
                Debug.LogWarning("[StageFlow] Playing 페이즈에서만 환자 강제 성공 처리를 실행할 수 있습니다.");
                return;
            }

            if (_patientTreatmentCoordinator == null || !_patientTreatmentCoordinator.HasActivePatientLoop)
            {
                Debug.LogWarning("[StageFlow] 현재 진행 중인 환자 루프가 없어 강제 성공 처리할 수 없습니다.");
                return;
            }

            Debug.Log("[StageFlow] 현재 환자를 강제로 성공 처리하고 다음 환자로 넘깁니다.");
            _patientTreatmentCoordinator.TryForceCompleteCurrentPatient();
        }

        // ── 타이머 동기화 ─────────────────────────────────────────────

        // 현재 마스터의 남은 시간을 모든 클라이언트에 동기화합니다.
        private void SyncTimerState()
        {
            _rpc.SetTimer(_timer.RemainingTime);
        }

        // ── 레시피 성공 미니게임 ──────────────────────────────────────

        // 정답 레시피 이후 집도의 대상 미니게임을 실행하고 결과를 반영합니다.
        private async UniTask<bool> RunRecipeMiniGame(CancellationToken ct)
        {
            MiniGameType type = MiniGameTypeExtensions.GetRandom();
            int surgeonActorNumber = GetMiniGameTargetActorNumber();
            Debug.Log($"[StageFlow]     레시피 미니게임 시작: {type} | 집도의 Actor {surgeonActorNumber}");

            bool success = _miniGameCoordinator != null &&
                await _miniGameCoordinator.RunRecipeMiniGame(surgeonActorNumber, type, ct);
            Debug.Log($"[StageFlow]     레시피 미니게임 결과: {(success ? "성공" : "실패")}");

            if (success)
            {
                float recoveredHealth = _patientStatusCoordinator != null
                    ? _patientStatusCoordinator.ApplyHeal(_miniGameSuccessHeal)
                    : 0f;
                Debug.Log($"[StageFlow]     미니게임 성공! 체력 +{_miniGameSuccessHeal} | 현재 체력: {recoveredHealth}");
                EventManager.Instance?.OnSurgerySuccess();
                return true;
            }

            float newHealth = _patientStatusCoordinator != null
                ? _patientStatusCoordinator.ApplyDamage(_miniGameFailPenalty)
                : 0f;
            Debug.Log($"[StageFlow]     미니게임 실패! 체력 -{_miniGameFailPenalty} | 현재 체력: {newHealth}");
            EventManager.Instance?.OnSurgeryFail(SurgeryFailureReason.MiniGameFailure);

            if (_isGameOver)
            {
                Debug.Log("[StageFlow]     환자 사망으로 게임 오버 처리");
                return false;
            }

            if (_emergencyPolicy.ShouldTriggerOnRecipeFail())
            {
                Debug.Log("[StageFlow]     미니게임 실패로 긴급 이벤트를 시작합니다.");

                if (_emergencyCoordinator != null &&
                    _emergencyCoordinator.TryStartEmergencyEvent(
                        EmergencyTriggerSource.MiniGameFail,
                        _rpc.CurrentPhase.Value,
                        _isGameOver,
                        _recipeProgressCoordinator != null && _recipeProgressCoordinator.IsWaitingForSubmission))
                {
                    await _emergencyCoordinator.WaitForResult(ct);
                }
            }

            return false;
        }


        // 미니게임을 수행할 집도의 ActorNumber를 계산합니다.
        private int GetMiniGameTargetActorNumber()
        {
            if (_rpc != null && _rpc.SurgeonActorNumber.Value > 0)
            {
                return _rpc.SurgeonActorNumber.Value;
            }

            if (PhotonNetwork.LocalPlayer != null)
            {
                return PhotonNetwork.LocalPlayer.ActorNumber;
            }

            return -1;
        }

        // 동기화된 스테이지 데이터를 갱신하고 외부 구독자에게 알립니다.
        private void HandleStageDataReceived(StageRuntimeData data)
        {
            _stageData = data;
            OnStageDataChanged?.Invoke(_stageData);
        }

        // 게임 오버 확정과 결과 전파는 결과 코디네이터에 위임합니다.
        private void TriggerGameOver(EGameOverReason reason)
        {
            _outcomeCoordinator?.TryTriggerGameOver(
                reason,
                _flowCts,
                RETURN_TO_WAITING_ROOM_DELAY_SEC,
                GAME_OVER_ACK_TOTAL_TIMEOUT_MS,
                ACK_TIMEOUT_MS);
        }

        // ── Update ──────────────────────────────────────────────────

        // 마스터가 플레이 중일 때 체력 드레인과 랜덤 응급 이벤트를 갱신합니다.
        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient || _isGameOver)
                return;

            if (_rpc.CurrentPhase.Value != EStagePhase.Playing)
                return;

            _patientStatusCoordinator?.Tick(Time.deltaTime);

            if (_isGameOver)
                return;

            if (_emergencyCoordinator != null && _emergencyCoordinator.TryHandleTimeout())
            {
                return;
            }

            if (_emergencyCoordinator != null &&
                _emergencyPolicy.ShouldTriggerRandom(Time.deltaTime))
            {
                _emergencyCoordinator.TryStartEmergencyEvent(
                    EmergencyTriggerSource.Random,
                    _rpc.CurrentPhase.Value,
                    _isGameOver,
                    _recipeProgressCoordinator != null && _recipeProgressCoordinator.IsWaitingForSubmission);
            }
        }

        // ── 정리 ────────────────────────────────────────────────────

        // 구독과 런타임 자원을 정리하며 StageFlow를 종료합니다.
        private void OnDestroy()
        {
            _flowCts?.Cancel();
            _flowCts?.Dispose();

            if (_onTimerExpired != null) _timer.OnExpired -= _onTimerExpired;
            if (_onTimerSyncTick != null) _timer.OnSyncTick -= _onTimerSyncTick;

            _trayHandler?.Dispose();
            _bootstrapCoordinator?.Dispose();
            _movementCoordinator?.Dispose();
            _outcomeCoordinator?.Dispose();
            _patientStatusCoordinator?.Dispose();
            _miniGameCoordinator?.Dispose();
            _emergencyCoordinator?.Dispose();
        }
    }
}
