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

    [RequireComponent(typeof(LLMService))]

    public class StageFlowManager : PunSingleton<StageFlowManager>,
        IStageFlowState,
        IStageFlowCommands,
        IStagePatientFlow,
        IStageMiniGameRunner,
        IStageOutcomeHost,
        IStageRecipeProgressHost,
        IStagePatientTreatmentHost,
        IStageMiniGameResolutionHost,
        IStageMiniGameResolutionDependencies
    {
        // ── 타이밍 상수 ─────────────────────────────────────────────
        private const int ACK_TIMEOUT_MS = 5000;
        private const int ACK_RETRY_DELAY_MS = 2000;
        private const int STAGE_DATA_ACK_TIMEOUT_MS = 10000;
        private const int GAME_OVER_ACK_TOTAL_TIMEOUT_MS = 12000;
        private const float STAGE_START_COUNTDOWN_SEC = 3f;
        private const float STAGE_CLEAR_DELAY_SEC = 5f;
        private const float PATIENT_TRANSITION_DELAY_SEC = 4f;
        private const float RETURN_TO_WAITING_ROOM_DELAY_SEC = 5f;
        private const float NO_SURGERY_TIMEOUT_SEC = 20f;

        // ── 위임 컴포넌트 ────────────────────────────────────────────
        [Header("핸들러")]
        [SerializeField] private StageFlowRpcHandler _rpc;
        [SerializeField] private StageTimer _timer;
        private EmergencyEventPolicy _emergencyPolicy = new();
        private LLMService _llmService;
        private StagePerformanceTracker _performanceTracker = new();

        [Header("미니게임 참조")]
        [SerializeField] private MiniGameLauncher _miniGameLauncher;



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
        public bool IsLocalSurgeon => GetLocalRole() == EStageRole.Surgeon;
        public bool IsEmergencyActive => _emergencyCoordinator != null && _emergencyCoordinator.IsActive;
        public bool IsEmergencyDiagnosisOperating => _emergencyCoordinator != null && _emergencyCoordinator.IsDiagnosisOperating;
        public float EmergencyDiagnosisOperationRemainingTime => _emergencyCoordinator?.DiagnosisOperationRemainingTime ?? 0f;
        public EmergencyEventKind CurrentEmergencyKind => _emergencyCoordinator?.CurrentKind ?? EmergencyEventKind.None;
        public float EmergencyRemainingTime => _emergencyCoordinator?.RemainingTime ?? 0f;
        public CraftedMaterialType CurrentEmergencyTrayTarget => _emergencyCoordinator?.CurrentTrayTarget ?? CraftedMaterialType.None;
        public DiagnosisScanType CurrentEmergencyDiagnosisTarget => _emergencyCoordinator?.CurrentDiagnosisTarget ?? DiagnosisScanType.None;
        public bool CanLocalInteractWithPatient =>
            _trayHandler != null &&
            _trayHandler.CanSubmit &&
            IsLocalSurgeon &&
            (_emergencyCoordinator == null || _emergencyCoordinator.CanSubmitTrayToPatient());

        public bool ShouldShowEmergencyUi =>
            IsLocalSurgeon &&
            IsEmergencyActive &&
            (_emergencyCoordinator == null || !_emergencyCoordinator.IsDiagnosisOperating) &&
            (_rpc == null || _rpc.CurrentPhase.Value == EStagePhase.Playing);


        public StagePerformanceTracker PerformanceTracker => _performanceTracker;
        public bool IsInitialized => _rpc != null;

        // ================================================================
        //  공개 조회 API
        // ================================================================

        // ── 역할 조회 ────────────────────────────────────────────────

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

        // ── 현재 치료 대상 조회 ──────────────────────────────────────

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

        // ================================================================
        //  코디네이터 호스트 구현
        // ================================================================

        // ── 공통 스테이지 상태 제공 ──────────────────────────────────
        StageRuntimeData IStageFlowState.StageData => _stageData;
        bool IStageFlowState.IsGameOver => _isGameOver;
        EStagePhase IStageFlowState.CurrentPhase => _rpc != null ? _rpc.CurrentPhase.Value : EStagePhase.None;
        bool IStageFlowState.IsWaitingForRecipeSubmission => _recipeProgressCoordinator != null && _recipeProgressCoordinator.IsWaitingForSubmission;
        float IStageFlowState.RemainingTime => _timer != null ? _timer.RemainingTime : 0f;
        float IStageFlowState.PatientHealth => _rpc != null ? _rpc.PatientHealth.Value : 0f;

        // ── 공통 스테이지 명령 제공 ──────────────────────────────────
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
        }

        void IStageFlowCommands.PublishReward(StageReward reward, StageResult result)
        {
            OnStageRewardGranted?.Invoke(reward, result);
        }

        // ── 환자 상태 제어 제공 ──────────────────────────────────────
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

        StageMiniGameCoordinator IStageMiniGameResolutionDependencies.MiniGameCoordinator => _miniGameCoordinator;
        StageEmergencyCoordinator IStageMiniGameResolutionDependencies.EmergencyCoordinator => _emergencyCoordinator;
        EmergencyEventPolicy IStageMiniGameResolutionDependencies.EmergencyPolicy => _emergencyPolicy;
        float IStageMiniGameResolutionDependencies.MiniGameFailPenalty => _stageData?.Settings?.MiniGameSettings?.FailPenalty ?? 0f;
        float IStageMiniGameResolutionDependencies.MiniGameSuccessHeal => _stageData?.Settings?.MiniGameSettings?.SuccessHeal ?? 0f;


        // ── 레시피 미니게임 실행 제공 ────────────────────────────────
        UniTask<bool> IStageMiniGameRunner.RunRecipeMiniGame(CancellationToken ct)
        {
            return _miniGameResolutionCoordinator != null
                ? _miniGameResolutionCoordinator.RunRecipeMiniGame(ct)
                : UniTask.FromResult(false);
        }

        // ── 런타임 상태 ────────────────────────────────────────────────
        private StageRuntimeData _stageData;
        private bool _isGameOver;
        private float _lastSurgeryTime;
        private bool _noSurgeryEventTriggered;

        // ── 런타임 컨트롤러 ───────────────────────────────────────────
        private StageRpcAckCoordinator _ackCoordinator;
        private StageBootstrapCoordinator _bootstrapCoordinator;
        private StageMiniGameCoordinator _miniGameCoordinator;
        private StageMiniGameResolutionCoordinator _miniGameResolutionCoordinator;
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
        public event Action<StageRuntimeData> OnStageDataChanged;
        public event Action<StageReward, StageResult> OnStageRewardGranted;


        protected override void Awake()
        {
            base.Awake();
            _llmService = GetComponent<LLMService>();
        }
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
            _ackCoordinator = new StageRpcAckCoordinator(ACK_RETRY_DELAY_MS, 10);
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
            _emergencyCoordinator = new StageEmergencyCoordinator(_rpc, () => _flowCts.Token, _stageData?.Settings, this);
            _movementCoordinator = new StageMovementCoordinator(_rpc);
            _outcomeCoordinator = new StageOutcomeCoordinator(_rpc, _ackCoordinator, this, new RewardLLMEvaluator(_llmService), new RewardMoneyPolicy(), new RewardSettlementService());
            _patientStatusCoordinator = new StagePatientStatusCoordinator(_patientHealthController, _rpc, TriggerGameOver);
            _miniGameResolutionCoordinator = new StageMiniGameResolutionCoordinator(_rpc, this, this);
            _recipeProgressCoordinator = new StageRecipeProgressCoordinator(_rpc, _trayHandler, _emergencyPolicy, _emergencyCoordinator, this);
            _patientTreatmentCoordinator = new StagePatientTreatmentCoordinator(_rpc, _timer, _emergencyPolicy, this, _recipeProgressCoordinator);

            _movementCoordinator.Initialize();

            _performanceTracker.Clear();

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
                bool stageStartSynchronized = await _bootstrapCoordinator.SynchronizeStageStart(_stageData, surgeonActor, STAGE_START_COUNTDOWN_SEC, ACK_TIMEOUT_MS, STAGE_DATA_ACK_TIMEOUT_MS, ct);
                if (!stageStartSynchronized)
                {
                    Debug.LogWarning("[StageFlow] 스테이지 시작 ACK가 아직 모두 오지 않았지만 게임은 계속 진행합니다.");
                }
                Debug.Log("[StageFlow] ▶ Playing 시작 (게임 루프 시작)");
                _rpc.SetPhase(EStagePhase.Playing);
                ResetSurgeryTimer();
                await _patientTreatmentCoordinator.RunGameLoop(PATIENT_TRANSITION_DELAY_SEC, ct);
                Debug.Log("[StageFlow] ✓ Playing 완료 (모든 환자 치료 성공)");

                Debug.Log("[StageFlow] ▶ StageClear! 5초 후 대기실 복귀 가능");
                _patientStatusCoordinator?.PauseDrain();
                _timer.Pause();
                SyncTimerState();
                _rpc.SetPhase(EStagePhase.StageClear);
                SelectRoleManager.Instance?.ClearRoles();

                await UniTask.Delay(TimeSpan.FromSeconds(STAGE_CLEAR_DELAY_SEC), cancellationToken: ct);

                Debug.Log("[StageFlow] 보상을 지급합니다.");
                await _outcomeCoordinator.ApplyReward();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StageFlow] 플로우 취소됨 (게임 오버)");
            }
        }

        // ================================================================
        //  공개 제출 API
        // ================================================================

        // 환자 상호작용으로 만들어진 제출 요청을 검증 루프로 전달합니다.
        public bool RequestTraySubmission(TrayItem trayItem, Action onAccepted, Action onRejected = null)
        {
            return _trayHandler.RequestSubmission(trayItem, onAccepted, onRejected);
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

#if UNITY_EDITOR
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
#endif
        // ── 타이머 동기화 ─────────────────────────────────────────────

        // 현재 마스터의 남은 시간을 모든 클라이언트에 동기화합니다.
        private void SyncTimerState()
        {
            _rpc.SetTimer(_timer.RemainingTime);
        }

        // ── 내부 보조 메서드 ─────────────────────────────────────────

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
            if (!CanRunPlayingUpdate(out EStagePhase currentPhase))
            {
                return;
            }

            _patientStatusCoordinator?.Tick(Time.deltaTime);

            if (_isGameOver)
            {
                return;
            }

            if (_emergencyCoordinator != null && _emergencyCoordinator.TryHandleTimeout())
            {
                return;
            }

            bool isWaitingForSubmission = _recipeProgressCoordinator != null && _recipeProgressCoordinator.IsWaitingForSubmission;
            if (_emergencyCoordinator != null &&
                _emergencyPolicy != null &&
                _emergencyPolicy.ShouldTriggerRandom(_stageData, Time.deltaTime))
            {
                _emergencyCoordinator.TryStartEmergencyEvent(
                    EmergencyTriggerSource.Random,
                    currentPhase,
                    _isGameOver,
                    isWaitingForSubmission);
            }

            CheckNoSurgeryTimeout();
        }

        private void CheckNoSurgeryTimeout()
        {
            if (_noSurgeryEventTriggered)
            {
                return;
            }

            if (Time.time - _lastSurgeryTime >= NO_SURGERY_TIMEOUT_SEC)
            {
                _noSurgeryEventTriggered = true;
                EventManager.Instance?.OnNoSurgery();
            }
        }

        public void ResetSurgeryTimer()
        {
            _lastSurgeryTime = Time.time;
            _noSurgeryEventTriggered = false;
        }

        private bool CanRunPlayingUpdate(out EStagePhase currentPhase)
        {
            currentPhase = EStagePhase.None;

            if (!PhotonNetwork.IsMasterClient || _isGameOver || _rpc == null)
            {
                return false;
            }

            currentPhase = _rpc.CurrentPhase.Value;
            return currentPhase == EStagePhase.Playing;
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
