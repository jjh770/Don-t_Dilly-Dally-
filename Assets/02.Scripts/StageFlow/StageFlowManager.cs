using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using DontDillyDally.MiniGame;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
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

    public class StageFlowManager : PunSingleton<StageFlowManager>
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
        public bool IsEmergencyActive => _emergencyController != null && _emergencyController.IsActive;
        public bool IsEmergencyDiagnosisOperating => _emergencyController != null && _emergencyController.IsDiagnosisOperating;
        public float EmergencyDiagnosisOperationRemainingTime => _emergencyController?.DiagnosisOperationRemainingTime ?? 0f;
        public EmergencyEventKind CurrentEmergencyKind => _emergencyController?.CurrentKind ?? EmergencyEventKind.None;
        public float EmergencyRemainingTime => _emergencyController?.RemainingTime ?? 0f;
        public CraftedMaterialType CurrentEmergencyTrayTarget => _emergencyController?.CurrentTrayTarget ?? CraftedMaterialType.None;
        public DiagnosisScanType CurrentEmergencyDiagnosisTarget => _emergencyController?.CurrentDiagnosisTarget ?? DiagnosisScanType.None;
        public bool CanLocalInteractWithPatient =>
            CanSubmitRecipeTray &&
            IsLocalSurgeon &&
            (_emergencyController == null || _emergencyController.CanSubmitTrayToPatient());

        public bool ShouldShowEmergencyUi =>
            IsLocalSurgeon &&
            IsEmergencyActive &&
            (_emergencyController == null || !_emergencyController.IsDiagnosisOperating) &&
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

        // ── 내부 상태 ────────────────────────────────────────────────
        private StageRuntimeData _stageData;
        private SurgeryRecipeJudge _recipeJudge;
        private PatientHealthController _patientHealthController;
        private CancellationTokenSource _flowCts;
        private EmergencyEventController _emergencyController;
        private bool _isWaitingForRecipeSubmission;
        private bool _isGameOver;

        // ── 트레이 제출 핸들러 ──────────────────────────────────────
        private TraySubmissionHandler _trayHandler;

        // ── 미니게임 결과 대기 ──────────────────────────────────────
        private UniTaskCompletionSource<bool> _miniGameResultTcs;
        private UniTaskCompletionSource<bool> _forcePatientSuccessTcs;

        // ── 긴급 이벤트 결과 대기 ──────────────────────────────────────
        private UniTaskCompletionSource<EmergencyResumeResult> _emergencyResultTcs;
        private CancellationTokenSource _diagnosisOperateCts;

        // ── 캐시된 델리게이트 (구독 해제용) ─────────────────────────
        private Action _onTimerExpired;
        private Action<float> _onTimerSyncTick;
        private Action<float> _onPatientHealthChanged;
        private Action _onPatientHealthDepleted;
        private Action<EGameOverReason> _onGameOverReceived;

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
            OnStageDataChanged?.Invoke(_stageData);
            _recipeJudge = new SurgeryRecipeJudge();
            _trayHandler = new TraySubmissionHandler(_rpc, () => _isGameOver);
            _patientHealthController = new PatientHealthController();

            // 타이머 이벤트 바인딩
            _onTimerExpired = () => TriggerGameOver(EGameOverReason.TimeExpired);
            _onTimerSyncTick = time => _rpc.SetTimer(time);
            _timer.OnExpired += _onTimerExpired;
            _timer.OnSyncTick += _onTimerSyncTick;

            // 환자 체력 이벤트 바인딩
            _onPatientHealthChanged = health => _rpc.SetHealth(health);
            _onPatientHealthDepleted = () => TriggerGameOver(EGameOverReason.PatientDeath);
            _patientHealthController.OnHealthChanged += _onPatientHealthChanged;
            _patientHealthController.OnHealthDepleted += _onPatientHealthDepleted;

            // 클라이언트 측 게임 오버, 스테이지 데이터 수신 (RPCHandler를 통해 로컬에서 수신)
            _onGameOverReceived = reason => OnGameOver?.Invoke(reason);
            _rpc.OnGameOverReceived += _onGameOverReceived;
            _rpc.OnStageDataReceived += HandleStageDataReceived;
            if (_rpc.TryGetLatestStageData(out StageRuntimeData receivedStageData))
            {
                HandleStageDataReceived(receivedStageData);
            }

            // 미니게임 RPC 수신
            _rpc.OnMiniGameRequested += HandleMiniGameRequested;
            _rpc.OnMiniGameResultReceived += HandleMiniGameResultReceived;
            _rpc.OnEmergencyMaterialSubmittedReceived += HandleEmergencyMaterialSubmitted;
            _rpc.OnEmergencyDiagnosisOperateReceived += HandleEmergencyDiagnosisOperateReceived;
            _rpc.OnEmergencyStartedReceived += HandleEmergencyStartedReceived;
            _rpc.OnEmergencyEndedReceived += HandleEmergencyEndedReceived;

            // 페이즈에 따른 플레이어 움직임 제어
            _rpc.CurrentPhase.Subscribe(OnPhaseChangedForMovement).AddTo(this);
            PlayerRegistry.OnPlayerRegistered += OnPlayerRegistered;

            // 보상 RPC 수신
            _rpc.OnStageRewardGrantedReceived += OnRewardGrantedReceived;

            // 긴급 이벤트 컨트롤러 초기화
            _emergencyController = new EmergencyEventController();
            _isWaitingForRecipeSubmission = false;

            _flowCts?.Cancel();
            _flowCts?.Dispose();
            _flowCts = new CancellationTokenSource();

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
                _rpc.SetPhase(EStagePhase.Loading);

                // 1. 집도의 동기화 (이미 컷씬에서 선정 완료 — RPC 전파만)
                int surgeonActor = StagePreloader.Instance.SurgeonActorNumber;
                Debug.Log($"[StageFlow] 집도의 동기화: Actor {surgeonActor}");
                await BroadcastAndWaitAck(
                    () => _rpc.SetSurgeon(surgeonActor),
                    handler => _rpc.OnSurgeonAckReceived += handler,
                    handler => _rpc.OnSurgeonAckReceived -= handler,
                    "집도의 동기화",
                    ACK_TIMEOUT_MS, ct);

                // 2. 스테이지 데이터 동기화 (이미 Preloader에서 생성 완료)
                Debug.Log("[StageFlow] 스테이지 데이터 동기화");
                string json = JsonUtility.ToJson(_stageData);
                await BroadcastAndWaitAck(
                    () => _rpc.BroadcastStageData(json),
                    handler => _rpc.OnStageDataAckReceived += handler,
                    handler => _rpc.OnStageDataAckReceived -= handler,
                    "스테이지 데이터",
                    STAGE_DATA_ACK_TIMEOUT_MS, ct);

                // 3. 게임 루프
                Debug.Log("[StageFlow] ▶ Playing 진입 (게임 카운트 다운 시작)");
                await RunStageStartCountdown(ct);
                Debug.Log("[StageFlow] ▶ Playing 시작 (게임 루프 시작)");
                _rpc.SetPhase(EStagePhase.Playing);
                await RunGameLoop(ct);
                Debug.Log("[StageFlow] ✓ Playing 완료 (모든 환자 치료 성공)");

                // 4. 스테이지 클리어
                Debug.Log("[StageFlow] ▶ StageClear! 5초 후 대기실 복귀 가능");
                PausePatientHealthDrain();
                _timer.Pause();
                SyncTimerState();
                _rpc.SetPhase(EStagePhase.StageClear);
                OnStageClear?.Invoke();
                SelectRoleManager.Instance?.ClearRoles();

                await UniTask.Delay(TimeSpan.FromSeconds(STAGE_CLEAR_DELAY_SEC), cancellationToken: ct);

                Debug.Log("[StageFlow] 보상을 지급합니다.");
                ApplyReward();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StageFlow] 플로우 취소됨 (게임 오버)");
            }
        }

        /// <summary>
        /// 공용 ACK 대기 헬퍼
        /// broadcast를 호출(나를 제외한 모두에게 전파)하고 모든 클라이언트의 ACK(Acknowledgement : 확답, 수신 확인)를 기다립니다.
        /// 타임아웃 시 retry를 1회 시도한 뒤 진행합니다.
        /// </summary>
        private async UniTask RunStageStartCountdown(CancellationToken ct)
        {
            double countdownStartTime = PhotonNetwork.Time;
            _rpc.StartCountdown(countdownStartTime, STAGE_START_COUNTDOWN_SEC);
            _rpc.SetPhase(EStagePhase.Countdown);

            Debug.Log($"[StageFlow] 시작 카운트다운: {STAGE_START_COUNTDOWN_SEC:0}초");
            await UniTask.Delay(TimeSpan.FromSeconds(STAGE_START_COUNTDOWN_SEC), cancellationToken: ct);
        }

        private async UniTask BroadcastAndWaitAck(
            Action broadcast,
            Action<Action<int>> subscribe,
            Action<Action<int>> unsubscribe,
            string label,
            int timeoutMs,
            CancellationToken ct)
        {
            int otherPlayerCount = PhotonNetwork.PlayerList.Length - 1;
            if (otherPlayerCount <= 0)
            {
                Debug.Log($"[StageFlow]   {label}: 다른 플레이어 없음, ACK 생략");
                broadcast();
                return;
            }

            HashSet<int> pendingActors = new HashSet<int>();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!player.IsLocal)
                {
                    pendingActors.Add(player.ActorNumber);
                }
            }

            UniTaskCompletionSource allAckTcs = new UniTaskCompletionSource();

            void OnAck(int actorNumber)
            {
                pendingActors.Remove(actorNumber);
                Debug.Log($"[StageFlow]   {label} ACK: Actor {actorNumber} (남은 {pendingActors.Count}명)");
                if (pendingActors.Count == 0)
                {
                    allAckTcs.TrySetResult();
                }
            }

            subscribe(OnAck);

            try
            {
                broadcast();

                // WhenAny -> 먼저 끝난 비동기 작업의 인덱스를 반환 : 0이면 allAckTcs.Task 먼저 완료, 1이면 timeoutMs 시간 초과
                bool completed = await UniTask.WhenAny(allAckTcs.Task, UniTask.Delay(timeoutMs, cancellationToken: ct)) == 0;

                if (completed)
                {
                    Debug.Log($"[StageFlow]   {label}: 모든 클라이언트 확인 완료");
                }
                else
                {
                    Debug.LogWarning($"[StageFlow]   {label}: ACK 타임아웃 ({pendingActors.Count}명 미응답) — 재전송. 미응답 Actor: {string.Join(", ", pendingActors)}");
                    broadcast();
                    await UniTask.Delay(ACK_RETRY_DELAY_MS, cancellationToken: ct);
                }
            }
            // 타임아웃나도 한번 더 broadcast() 실행 후 응답 없으면 이벤트 구독 해제
            finally
            {
                unsubscribe(OnAck);
            }
        }

        // ── 게임 루프 ───────────────────────────────────────────────

        // 스테이지 타이머를 돌리면서 환자 단위 메인 루프를 실행합니다.
        private async UniTask RunGameLoop(CancellationToken ct)
        {
            _timer.Set(_stageData.TotalTimeLimitSec);
            _timer.Resume();
            SyncTimerState();
            Debug.Log($"[StageFlow]   타이머 시작: {_stageData.TotalTimeLimitSec}초 | 환자 {_stageData.Patients.Count}명 치료 시작");

            for (int i = 0; i < _stageData.Patients.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                _rpc.SetPatientIndex(i);

                // 2번째 환자부터 전환 연출
                if (i > 0)
                {
                    Debug.Log("[StageFlow]   환자 전환 중... (타이머 일시정지)");
                    PausePatientHealthDrain();
                    _timer.Pause();
                    SyncTimerState();
                    _rpc.SetPhase(EStagePhase.PatientTransition);
                    await UniTask.Delay(TimeSpan.FromSeconds(PATIENT_TRANSITION_DELAY_SEC), cancellationToken: ct);
                    _rpc.SetPhase(EStagePhase.Playing);
                    _timer.Resume();
                    ResumePatientHealthDrain();
                    SyncTimerState();
                    Debug.Log("[StageFlow]   환자 전환 완료 (타이머 재개)");
                }

                await RunPatientLoop(i, ct);
            }
        }

        // 현재 환자 한 명에 대한 치료 루프를 준비하고 실행합니다.
        private async UniTask RunPatientLoop(int patientIndex, CancellationToken ct)
        {
            DiseaseData disease = _stageData.Patients[patientIndex];
            Debug.Log($"[StageFlow] ── 환자 {patientIndex + 1}/{_stageData.Patients.Count} 시작 | 병명: {disease.DiseaseName} | 레시피: {disease.Recipes?.Count ?? 0}단계 | 체력: {_stageData.MaxPatientHealth}");

            CommentaryController.Instance?.SetCurrentPatientIndex(patientIndex);
            EventManager.Instance?.OnNewPatientAppeared(disease.PatientName, disease.DiseaseName);

            _recipeJudge.SetDisease(disease);
            InitializePatientHealth();
            _emergencyPolicy.ResetTimer();
            _forcePatientSuccessTcs = new UniTaskCompletionSource<bool>();

            try
            {
                await RunRecipeLoop(disease, ct);
            }
            finally
            {
                _forcePatientSuccessTcs = null;
            }

            Debug.Log($"[StageFlow] ── 환자 {patientIndex + 1}/{_stageData.Patients.Count} 치료 완료! | 병명: {disease.DiseaseName} | 남은 체력: {_rpc.PatientHealth.Value}");
            _stageData.SavedCount += 1;
        }

        // 한 환자의 레시피를 순서대로 제출받고 판정합니다.
        private async UniTask RunRecipeLoop(DiseaseData disease, CancellationToken ct)
        {
            int recipeIndex = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                _rpc.SetRecipeIndex(recipeIndex);
                Debug.Log($"[StageFlow]     레시피 {recipeIndex + 1} 대기 중... (트레이 제출 대기)");

                _isWaitingForRecipeSubmission = true;
                UniTask<SubmittedTray> waitForTrayTask = _trayHandler.WaitForSubmission(ct);
                UniTask<bool> forceSuccessTask = _forcePatientSuccessTcs != null
                    ? _forcePatientSuccessTcs.Task
                    : UniTask.Never<bool>(ct);

                var (completedTaskIndex, tray, _) = await UniTask.WhenAny(waitForTrayTask, forceSuccessTask);
                _isWaitingForRecipeSubmission = false;
                if (completedTaskIndex == 1)
                {
                    Debug.Log("[StageFlow]     디버그 요청으로 현재 환자를 성공 처리합니다.");
                    return;
                }
                Debug.Log("[StageFlow]     트레이 제출됨 → 판정 중...");

                SurgeryJudgeResult result = _recipeJudge.JudgeNextRecipe(tray, _rpc.PatientHealth.Value);

                if (result.Success)
                {
                    Debug.Log($"[StageFlow]     ✓ 레시피 {recipeIndex + 1} 성공! (ID: {result.CompletedRecipeId}) | 질병완치={result.DiseaseCured}");

                    // 집도의에게만 레시피 성공 후 수술 미니게임 권한을 부여합니다.
                    // OnSurgerySuccess/OnSurgeryFail은 RunRecipeMiniGame 내부에서 미니게임 결과에 따라 호출됩니다.
                    bool shouldAdvanceRecipe = await RunRecipeMiniGame(ct);

                    if (result.DiseaseCured)
                    {
                        Debug.Log("[StageFlow]     ★ 질병 완치!");
                        return;
                    }

                    if (shouldAdvanceRecipe)
                    {
                        recipeIndex++;
                    }
                }
                else if (result.SurgeryFailure == SurgeryFailureReason.RecipeMismatch)
                {
                    float newHealth = ApplyPatientDamage(disease.FailHealthPenalty);
                    Debug.Log($"[StageFlow]     ✗ 레시피 실패! 체력 -{disease.FailHealthPenalty} → 현재 체력: {newHealth}");
                    EventManager.Instance?.OnSurgeryFail(result.SurgeryFailure);

                    if (_isGameOver)
                    {
                        Debug.Log("[StageFlow]     !! 환자 사망 → 게임 오버");
                        return;
                    }

                    if (_emergencyPolicy.ShouldTriggerOnRecipeFail())
                    {
                        Debug.Log("[StageFlow]     ⚡ 긴급 이벤트 발동!");

                        if (TryStartEmergencyEvent(EmergencyTriggerSource.RecipeFail))
                        {
                            EmergencyResumeResult emergencyResult = await WaitForEmergencyResult(ct);
                            if (emergencyResult == EmergencyResumeResult.AdvanceToNextRecipe)
                            {
                                recipeIndex++;
                            }

                            continue;
                        }
                    }
                }
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
            if (_emergencyController == null || !_emergencyController.IsActive)
            {
                return false;
            }

            if (_emergencyController.CurrentKind != EmergencyEventKind.Tray)
            {
                return false;
            }

            if (materialType == CraftedMaterialType.None)
            {
                return false;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                HandleEmergencyMaterialSubmitted(materialType, itemViewId, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
            }
            else
            {
                _rpc.SubmitEmergencyMaterial(materialType, itemViewId);
            }

            return true;
        }

        public bool RequestDiagnosisOperation(DiagnosisScanType diagnosisType)
        {
            if (_emergencyController == null || !_emergencyController.IsActive)
            {
                return false;
            }

            if (_emergencyController.CurrentKind != EmergencyEventKind.Diagnosis)
            {
                return false;
            }

            if (diagnosisType == DiagnosisScanType.None)
            {
                return false;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                HandleEmergencyDiagnosisOperateReceived(diagnosisType, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
            }
            else
            {
                _emergencyController.TryBeginDiagnosisOperationLocally(diagnosisType);
                _rpc.SubmitEmergencyDiagnosisOperate(diagnosisType);
            }

            return true;
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

            if (_forcePatientSuccessTcs == null)
            {
                Debug.LogWarning("[StageFlow] 현재 진행 중인 환자 루프가 없어 강제 성공 처리할 수 없습니다.");
                return;
            }

            Debug.Log("[StageFlow] 현재 환자를 강제로 성공 처리하고 다음 환자로 넘깁니다.");
            _forcePatientSuccessTcs.TrySetResult(true);
        }

        // ── 타이머 동기화 ─────────────────────────────────────────────

        // 현재 마스터의 남은 시간을 모든 클라이언트에 동기화합니다.
        private void SyncTimerState()
        {
            _rpc.SetTimer(_timer.RemainingTime);
        }

        // ── 환자 체력 동기화 ─────────────────────────────────────────────
        // 환자 체력 컨트롤러를 현재 스테이지 설정값으로 초기화합니다.
        private void InitializePatientHealth()
        {
            if (_patientHealthController == null || _stageData == null)
            {
                return;
            }

            _patientHealthController.Initialize(
                _stageData.MaxPatientHealth,
                _stageData.PatientHealthDrainPerSecond);

            if (_rpc.CurrentPhase.Value == EStagePhase.Playing)
            {
                _patientHealthController.ResumeDrain();
            }
        }

        // 환자 체력을 즉시 감소시키고 최신 체력을 반환합니다.
        private float ApplyPatientDamage(float damage)
        {
            if (_patientHealthController == null)
            {
                return _rpc.PatientHealth.Value;
            }

            _patientHealthController.ApplyDamage(damage);
            return _rpc.PatientHealth.Value;
        }

        // 환자 체력을 회복시키고 최신 체력을 반환합니다.
        private float RecoverPatientHealth(float heal)
        {
            if (_patientHealthController == null)
            {
                return _rpc.PatientHealth.Value;
            }

            _patientHealthController.ApplyHeal(heal);
            return _rpc.PatientHealth.Value;
        }

        // 자연 감소 중인 환자 체력 드레인을 일시 정지합니다.
        private void PausePatientHealthDrain()
        {
            _patientHealthController?.PauseDrain();
        }

        // 플레이 중일 때 환자 체력 자연 감소를 다시 시작합니다.
        private void ResumePatientHealthDrain()
        {
            if (_rpc.CurrentPhase.Value == EStagePhase.Playing)
            {
                _patientHealthController?.ResumeDrain();
            }
        }

        // ── 레시피 성공 미니게임 ──────────────────────────────────────

        // 정답 레시피 이후 집도의 대상 미니게임을 실행하고 결과를 반영합니다.
        private async UniTask<bool> RunRecipeMiniGame(CancellationToken ct)
        {
            MiniGameType type = MiniGameTypeExtensions.GetRandom();
            int surgeonActorNumber = GetMiniGameTargetActorNumber();
            Debug.Log($"[StageFlow]     레시피 미니게임 시작: {type} | 집도의 Actor {surgeonActorNumber}");

            bool success = await LaunchRoleMiniGame(surgeonActorNumber, type, ct);
            Debug.Log($"[StageFlow]     레시피 미니게임 결과: {(success ? "성공" : "실패")}");

            if (success)
            {
                float recoveredHealth = RecoverPatientHealth(_miniGameSuccessHeal);
                Debug.Log($"[StageFlow]     미니게임 성공! 체력 +{_miniGameSuccessHeal} | 현재 체력: {recoveredHealth}");
                EventManager.Instance?.OnSurgerySuccess();
                return true;
            }

            float newHealth = ApplyPatientDamage(_miniGameFailPenalty);
            Debug.Log($"[StageFlow]     미니게임 실패! 체력 -{_miniGameFailPenalty} | 현재 체력: {newHealth}");
            EventManager.Instance?.OnSurgeryFail(SurgeryFailureReason.MiniGameFailure);

            if (_isGameOver)
            {
                Debug.Log("[StageFlow]     !! 환자 사망 -> 게임 오버");
                return false;
            }

            if (_emergencyPolicy.ShouldTriggerOnRecipeFail())
            {
                Debug.Log("[StageFlow]     ⚡ 미니게임 실패 -> 긴급 이벤트 발동!");

                if (TryStartEmergencyEvent(EmergencyTriggerSource.MiniGameFail))
                {
                    EmergencyResumeResult emergencyResult = await WaitForEmergencyResult(ct);
                    return emergencyResult == EmergencyResumeResult.AdvanceToNextRecipe;
                }
            }

            return false;
        }


        // 현재 클라이언트에서 직접 미니게임을 실행하고 성공 여부를 반환합니다.
        private async UniTask<bool> LaunchLocalMiniGame(MiniGameType type, CancellationToken ct)
        {
            if (_miniGameLauncher == null)
            {
                Debug.LogWarning("[StageFlow] 로컬 미니게임 런처가 없어 실패로 처리합니다.");
                return false;
            }

            if (_miniGameLauncher.IsPlaying)
            {
                Debug.LogWarning("[StageFlow] 이미 실행 중인 미니게임이 있어 실패로 처리합니다.");
                return false;
            }

            var tcs = new UniTaskCompletionSource<MiniGameResult>(); // 미니게임 결과 대기
            using (ct.Register(() => tcs.TrySetCanceled())) // 만약 StageFlow가 중간에 취소되면 tcs.Task도 취소 상태로 바뀌게 연결
            {
                _miniGameLauncher.Launch(type, result => tcs.TrySetResult(result));
                MiniGameResult result = await tcs.Task;
                return result.IsSuccess;
            }
        }

        // 대상 역할 플레이어가 로컬인지 원격인지에 따라 미니게임 실행 경로를 분기합니다.
        private async UniTask<bool> LaunchRoleMiniGame(int targetActorNumber, MiniGameType type, CancellationToken ct)
        {
            if (PhotonNetwork.LocalPlayer != null && targetActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return await LaunchLocalMiniGame(type, ct);
            }

            return await LaunchRemoteMiniGame(targetActorNumber, type, ct);
        }

        // 원격 집도의에게 미니게임 실행을 요청하고 결과를 기다립니다.
        private async UniTask<bool> LaunchRemoteMiniGame(int targetActorNumber, MiniGameType type, CancellationToken ct)
        {
            if (targetActorNumber <= 0)
            {
                Debug.LogWarning("[StageFlow] 원격 미니게임 대상이 없어 실패로 처리합니다.");
                return false;
            }

            _miniGameResultTcs = new UniTaskCompletionSource<bool>();
            using (ct.Register(() => _miniGameResultTcs.TrySetCanceled()))
            {
                _rpc.RequestMiniGame(targetActorNumber, type);
                return await _miniGameResultTcs.Task;
            }
        }

        /// <summary>
        /// 비-마스터 클라이언트에서 미니게임 RPC 수신 시 호출됩니다.
        // 원격에서 요청한 미니게임을 로컬 집도의 클라이언트가 실행합니다.
        /// </summary>
        private void HandleMiniGameRequested(MiniGameType type)
        {
            if (!IsLocalSurgeon)
            {
                Debug.LogWarning("[StageFlow] 집도의가 아닌 클라이언트가 미니게임 요청을 받아 실패로 응답합니다.");
                _rpc.SendMiniGameResult(false);
                return;
            }

            if (_miniGameLauncher == null)
            {
                Debug.LogWarning("[StageFlow] 로컬 미니게임 런처가 없어 실패로 응답합니다.");
                _rpc.SendMiniGameResult(false);
                return;
            }

            if (_miniGameLauncher.IsPlaying)
            {
                Debug.LogWarning("[StageFlow] 이미 실행 중인 미니게임이 있어 실패로 응답합니다.");
                _rpc.SendMiniGameResult(false);
                return;
            }

            Debug.Log($"[StageFlow] 미니게임 요청 수신 → 로컬 실행: {type}");
            _miniGameLauncher.Launch(type, result =>
            {
                _rpc.SendMiniGameResult(result.IsSuccess);
            });
        }

        /// <summary>
        /// 마스터에서 원격 미니게임 결과 RPC 수신 시 호출됩니다.
        // 원격 미니게임 결과를 마스터의 대기 중인 Task에 전달합니다.
        /// </summary>
        private void HandleMiniGameResultReceived(bool success)
        {
            _miniGameResultTcs?.TrySetResult(success);
        }


        // 현재 미니게임을 수행해야 할 집도의 ActorNumber를 반환합니다.
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

        // ── 긴급 이벤트 ─────────────────────────────────────────────

        // 
        private EmergencyEventKind SelectEmergencyKind()
        {
            return UnityEngine.Random.value < 0.5f
                ? EmergencyEventKind.Tray
                : EmergencyEventKind.Diagnosis;
        }

        private bool TryStartEmergencyEvent(EmergencyTriggerSource triggerSource)
        {
            if (_emergencyController == null || _rpc == null)
                return false;

            if (!_emergencyController.CanBegin(
                    _rpc.CurrentPhase.Value,
                    _isGameOver,
                    _isWaitingForRecipeSubmission,
                    triggerSource))
            {
                return false;
            }

            EmergencyEventKind kind = SelectEmergencyKind();
            if (!_emergencyController.TryBegin(kind, triggerSource))
                return false;

            EventManager.Instance?.OnPatientCritical("긴급 처치가 필요합니다.");
            _rpc.BroadcastEmergency(
                kind,
                triggerSource,
                _emergencyController.CurrentTrayTarget,
                _emergencyController.CurrentDiagnosisTarget);

            Debug.Log($"[StageFlow] 긴급 이벤트 시작: {kind} | 원인: {triggerSource}");
            return true;
        }

        private async UniTask<EmergencyResumeResult> WaitForEmergencyResult(CancellationToken ct)
        {
            _emergencyResultTcs?.TrySetCanceled();
            _emergencyResultTcs = new UniTaskCompletionSource<EmergencyResumeResult>();

            using (ct.Register(() => _emergencyResultTcs.TrySetCanceled()))
            {
                return await _emergencyResultTcs.Task;
            }
        }

        private void CompleteEmergencySuccess()
        {
            if (_emergencyController == null || !_emergencyController.IsActive)
                return;

            CancelDiagnosisOperateTask();
            EmergencyResumeResult result = _emergencyController.ResolveSuccess();
            _rpc?.BroadcastEmergencyEnd();
            _emergencyResultTcs?.TrySetResult(result);
            _emergencyResultTcs = null;
        }

        private void CompleteEmergencyFailure()
        {
            if (_emergencyController == null || !_emergencyController.IsActive)
                return;

            CancelDiagnosisOperateTask();
            EmergencyResumeResult result = _emergencyController.ResolveFailure();
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
            if (PhotonNetwork.IsMasterClient || _emergencyController == null)
            {
                return;
            }

            _emergencyController.SyncBegin(kind, triggerSource, trayTarget, diagnosisTarget);
        }

        private void HandleEmergencyEndedReceived()
        {
            if (PhotonNetwork.IsMasterClient || _emergencyController == null)
            {
                return;
            }

            _emergencyController.End();
        }

        private void HandleEmergencyMaterialSubmitted(CraftedMaterialType materialType, int itemViewId, int submitterActorNumber)
        {
            if (!PhotonNetwork.IsMasterClient || _emergencyController == null || !_emergencyController.IsActive)
            {
                return;
            }

            if (_emergencyController.CurrentKind != EmergencyEventKind.Tray)
            {
                return;
            }

            if (_emergencyController.EvaluateEmergencyMaterialSubmission(materialType))
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
            if (!PhotonNetwork.IsMasterClient || _emergencyController == null || !_emergencyController.IsActive)
            {
                return;
            }

            if (_emergencyController.CurrentKind != EmergencyEventKind.Diagnosis)
            {
                return;
            }

            EmergencyDiagnosisOperationResult result = _emergencyController.TryBeginDiagnosisOperation(diagnosisType);
            switch (result)
            {
                case EmergencyDiagnosisOperationResult.Started:
                    Debug.Log($"[StageFlow] Diagnosis emergency started: {diagnosisType} | Actor {submitterActorNumber}");
                    RunDiagnosisEmergencySuccess().Forget();
                    break;

                case EmergencyDiagnosisOperationResult.Failed:
                    Debug.Log($"[StageFlow] Diagnosis emergency failed: {diagnosisType} | Actor {submitterActorNumber}");
                    CompleteEmergencyFailure();
                    break;
            }
        }

        private async UniTaskVoid RunDiagnosisEmergencySuccess()
        {
            CancelDiagnosisOperateTask();
            _diagnosisOperateCts = CancellationTokenSource.CreateLinkedTokenSource(_flowCts.Token);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(5f), cancellationToken: _diagnosisOperateCts.Token);

                if (_emergencyController != null &&
                    _emergencyController.IsActive &&
                    _emergencyController.CurrentKind == EmergencyEventKind.Diagnosis &&
                    _emergencyController.IsDiagnosisOperating)
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

        private void SyncSubmittedTrayReset(int trayViewId)
        {
            if (trayViewId < 0)
            {
                return;
            }

            PhotonView trayView = PhotonView.Find(trayViewId);
            if (trayView == null || !trayView.TryGetComponent(out TrayItem trayItem))
            {
                return;
            }

            trayItem.ClearContentsAndSync();
        }

        // ── 게임 오버 ───────────────────────────────────────────────

        // 게임오버 상태를 확정하고 모든 진행을 중단합니다.
        private void TriggerGameOver(EGameOverReason reason)
        {
            if (_isGameOver) return;
            _isGameOver = true;

            Debug.Log($"[StageFlow] ========== 게임 오버: {reason} ==========");

            _flowCts?.Cancel();
            PausePatientHealthDrain();
            _timer.Pause();
            SyncTimerState();

            _rpc.SetPhase(EStagePhase.GameOver);

            OnGameOver?.Invoke(reason);

            // 코멘터리 발행
            if (reason == EGameOverReason.PatientDeath)
            {
                EventManager.Instance?.OnPatientDeath();
            }
            else if (reason == EGameOverReason.TimeExpired)
            {
                EventManager.Instance?.OnTimeOut();
            }

            Debug.Log($"[StageFlow] 게임 오버: {reason} | 남은 타이머: {_timer.RemainingTime:F1}초 | 5초 후 대기실 복귀 가능");

            // 역할 시각 표시 초기화
            SelectRoleManager.Instance?.ClearRoles();

            BroadcastGameOverAndWaitAck(reason).Forget();
        }

        // 게임오버 사실을 모든 클라이언트에 전송하고 ACK를 기다립니다.
        private async UniTaskVoid BroadcastGameOverAndWaitAck(EGameOverReason reason)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(GAME_OVER_ACK_TOTAL_TIMEOUT_MS);

            try
            {
                await BroadcastAndWaitAck(
                    () => _rpc.BroadcastGameOver(reason),
                    handler => _rpc.OnGameOverAckReceived += handler,
                    handler => _rpc.OnGameOverAckReceived -= handler,
                    "게임 오버",
                    ACK_TIMEOUT_MS, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[StageFlow] 게임 오버 ACK 대기 중 타임아웃");
            }

            await UniTask.Delay(TimeSpan.FromSeconds(RETURN_TO_WAITING_ROOM_DELAY_SEC));

            // 보상을 처리합니다.
            ApplyReward();
        }


        // ── 플레이어 움직임 제어 ──────────────────────────────────────

        // 현재 페이즈에 따라 모든 플레이어 이동 가능 여부를 갱신합니다.
        private void OnPhaseChangedForMovement(EStagePhase phase)
        {
            bool canMove = phase == EStagePhase.Playing;
            SetAllPlayersMovementLocked(!canMove);
        }

        // 새로 등록된 플레이어에게 현재 이동 잠금 상태를 즉시 반영합니다.
        private void OnPlayerRegistered(PlayerController player)
        {
            if (player != null && player.MovementAbility != null)
            {
                bool canMove = _rpc.CurrentPhase.Value == EStagePhase.Playing;
                player.MovementAbility.SetMovementLocked(!canMove);
            }
        }

        // 현재 씬의 모든 플레이어 이동을 일괄 잠그거나 해제합니다.
        private void HandleStageDataReceived(StageRuntimeData data)
        {
            _stageData = data;
            OnStageDataChanged?.Invoke(_stageData);
        }

        private void SetAllPlayersMovementLocked(bool locked)
        {
            foreach (PlayerController player in PlayerRegistry.GetAllPlayers())
            {
                if (player != null && player.MovementAbility != null)
                {
                    player.MovementAbility.SetMovementLocked(locked);
                }
            }
        }

        // 결과에 따른 보상을 처리합니다.
        private void ApplyReward()
        {
            var result = new StageResult(
                savedCount: _stageData.SavedCount,
                patientCount: _stageData.PatientCount,
                difficulty: _stageData.Difficulty
            );

            StageReward reward = RoomDataManager.Instance.ApplyReward(_stageData.StageId, result);

            // 모든 클라이언트에게 전파
            _rpc.BroadcastStageReward(reward, result);

            Debug.Log($"별: {reward.Stars} / 돈: {reward.Money} / 신기록: {reward.IsNewBest}");
            // → UI 연출로 넘기기
        }

        public void OnRewardGrantedReceived(StageReward reward, StageResult result)
        {
            OnStageRewardGranted?.Invoke(reward, result);
        }

        // ── Update ──────────────────────────────────────────────────

        // 마스터가 플레이 중일 때 체력 드레인과 랜덤 응급 이벤트를 갱신합니다.
        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient || _isGameOver)
                return;

            if (_rpc.CurrentPhase.Value != EStagePhase.Playing)
                return;

            _patientHealthController?.Tick(Time.deltaTime);

            if (_isGameOver)
                return;

            if (_emergencyController != null && _emergencyController.HasTimedOut())
            {
                Debug.Log("[StageFlow] 긴급 이벤트 시간 초과");
                CompleteEmergencyFailure();
                return;
            }

            if (_emergencyPolicy.ShouldTriggerRandom(Time.deltaTime))
            {
                TryStartEmergencyEvent(EmergencyTriggerSource.Random);
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

            if (_rpc != null)
            {
                if (_onGameOverReceived != null) _rpc.OnGameOverReceived -= _onGameOverReceived;
                _rpc.OnStageDataReceived -= HandleStageDataReceived;
                _rpc.OnMiniGameRequested -= HandleMiniGameRequested;
                _rpc.OnMiniGameResultReceived -= HandleMiniGameResultReceived;
                _rpc.OnEmergencyMaterialSubmittedReceived -= HandleEmergencyMaterialSubmitted;
                _rpc.OnEmergencyDiagnosisOperateReceived -= HandleEmergencyDiagnosisOperateReceived;
                _rpc.OnEmergencyStartedReceived -= HandleEmergencyStartedReceived;
                _rpc.OnEmergencyEndedReceived -= HandleEmergencyEndedReceived;
                _rpc.OnStageRewardGrantedReceived -= OnRewardGrantedReceived;
            }

            PlayerRegistry.OnPlayerRegistered -= OnPlayerRegistered;

            if (_patientHealthController != null)
            {
                if (_onPatientHealthChanged != null) _patientHealthController.OnHealthChanged -= _onPatientHealthChanged;
                if (_onPatientHealthDepleted != null) _patientHealthController.OnHealthDepleted -= _onPatientHealthDepleted;
                _patientHealthController.Reset();
            }

            _trayHandler?.Dispose();
            CancelDiagnosisOperateTask();
        }
    }
}
