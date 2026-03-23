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
        // ── 위임 컴포넌트 ────────────────────────────────────────────
        [Header("핸들러")]
        [SerializeField] private StageFlowRpcHandler _rpc;
        [SerializeField] private StageTimer _timer;
        [SerializeField] private EmergencyEventHandler _emergencyHandler;

        [Header("미니게임")]
        [SerializeField] private float _miniGameFailPenalty = 10f;

        [Header("디버그")]
        [SerializeField] private bool _debugMode;
        [Tooltip("디버그 모드에서 T키로 트레이 자동 제출 (정답), Y키로 오답 제출")]
        [SerializeField] private bool _debugAutoSubmit;

        // ── 외부 구독용 (RpcHandler에 위임) ──────────────────────────
        public IReadOnlyReactiveProperty<EStagePhase> CurrentPhase => _rpc.CurrentPhase;
        public IReadOnlyReactiveProperty<float> PatientHealth => _rpc.PatientHealth;
        public IReadOnlyReactiveProperty<float> StageTimer => _rpc.StageTimer;
        public IReadOnlyReactiveProperty<int> CurrentPatientIndex => _rpc.CurrentPatientIndex;
        public IReadOnlyReactiveProperty<int> CurrentRecipeIndex => _rpc.CurrentRecipeIndex;
        public IReadOnlyReactiveProperty<int> SurgeonActorNumber => _rpc.SurgeonActorNumber;
        public StageData CurrentStageData => _stageData;
        public float LocalRemainingTime => _timer != null ? _timer.RemainingTime : 0f;
        public TraySubmissionHandler TrayHandler => _trayHandler;
        public bool CanSubmitRecipeTray => _trayHandler != null && _trayHandler.CanSubmit;
        public EStageRole LocalRole => GetLocalRole();
        public bool IsLocalSurgeon => LocalRole == EStageRole.Surgeon;
        public bool IsLocalAssistant => LocalRole == EStageRole.Assistant;
        public bool CanLocalInteractWithPatient => CanSubmitRecipeTray && IsLocalSurgeon;

        public EStageRole GetLocalRole()
        {
            if (!PhotonNetwork.InRoom ||
                PhotonNetwork.LocalPlayer == null)
            {
                return EStageRole.None;
            }

            return GetRoleForActorNumber(PhotonNetwork.LocalPlayer.ActorNumber);
        }

        public EStageRole GetRoleForActorNumber(int actorNumber)
        {
            if (_rpc == null ||
                actorNumber <= 0 ||
                _rpc.SurgeonActorNumber.Value <= 0)
            {
                return EStageRole.None;
            }

            return _rpc.SurgeonActorNumber.Value == actorNumber
                ? EStageRole.Surgeon
                : EStageRole.Assistant;
        }

        public string GetLocalRoleDisplayName()
        {
            return LocalRole switch
            {
                EStageRole.Surgeon => "집도의",
                EStageRole.Assistant => "어시스트",
                _ => "역할 미정"
            };
        }

        public bool TryGetCurrentDisease(out DiseaseData disease)
        {
            disease = null;

            if (_stageData == null ||
                _stageData.Patients == null ||
                _rpc == null)
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

        public bool TryGetCurrentRecipe(out RecipeData recipe)
        {
            recipe = null;

            if (!TryGetCurrentDisease(out DiseaseData disease) ||
                disease.Recipes == null ||
                disease.Recipes.Count == 0 ||
                _rpc == null)
            {
                return false;
            }

            int currentRecipeOrder = _rpc.CurrentRecipeIndex.Value;
            int selectedIndex = -1;
            int minOrder = int.MaxValue;

            for (int i = 0; i < disease.Recipes.Count; i++)
            {
                RecipeData candidate = disease.Recipes[i];
                if (candidate == null ||
                    candidate.Order < currentRecipeOrder ||
                    candidate.Order >= minOrder)
                {
                    continue;
                }

                minOrder = candidate.Order;
                selectedIndex = i;
            }

            if (selectedIndex < 0)
            {
                return false;
            }

            recipe = disease.Recipes[selectedIndex];
            return recipe != null;
        }

        // ── 내부 상태 ────────────────────────────────────────────────
        private StageData _stageData;
        private TreatmentJudgeManager _judgeManager;
        private PatientHealthController _patientHealthController;
        private CancellationTokenSource _flowCts;
        private bool _isGameOver;

        // ── 외부 참조 (Bootstrapper에서 주입) ────────────────────────
        private DiseaseGenerationManager _diseaseGenManager;
        private MiniGameLauncher _miniGameLauncher;

        // ── 트레이 제출 핸들러 ──────────────────────────────────────
        private TraySubmissionHandler _trayHandler;

        // ── 미니게임 결과 대기 ──────────────────────────────────────
        private UniTaskCompletionSource<bool> _miniGameResultTcs;

        // ── 캐시된 델리게이트 (구독 해제용) ─────────────────────────
        private Action _onTimerExpired;
        private Action<float> _onTimerSyncTick;
        private Action<float> _onPatientHealthChanged;
        private Action _onPatientHealthDepleted;

        // ── 이벤트 ──────────────────────────────────────────────────
        public event Action<EGameOverReason> OnGameOver;
        public event Action OnStageClear;

        // ================================================================
        //  초기화
        // ================================================================

        public void Initialize(DiseaseGenerationManager diseaseGenManager, MiniGameLauncher miniGameLauncher, StageData stageData)
        {
            _diseaseGenManager = diseaseGenManager;
            _miniGameLauncher = miniGameLauncher;
            _stageData = stageData;
            _judgeManager = new TreatmentJudgeManager();
            _trayHandler = new TraySubmissionHandler(_rpc, () => _isGameOver);
            _patientHealthController = new PatientHealthController();

            // 타이머 이벤트 바인딩
            _onTimerExpired = () => TriggerGameOver(EGameOverReason.TimeExpired);
            _onTimerSyncTick = time => _rpc.SetTimer(time);
            _timer.OnExpired += _onTimerExpired;
            _timer.OnSyncTick += _onTimerSyncTick;

            _onPatientHealthChanged = health => _rpc.SetHealth(health);
            _onPatientHealthDepleted = () => TriggerGameOver(EGameOverReason.PatientDeath);
            _patientHealthController.OnHealthChanged += _onPatientHealthChanged;
            _patientHealthController.OnHealthDepleted += _onPatientHealthDepleted;

            // 클라이언트 측 게임 오버 수신
            _rpc.OnGameOverReceived += reason => OnGameOver?.Invoke(reason);
            _rpc.OnStageDataReceived += data => _stageData = data;

            // 미니게임 RPC 수신
            _rpc.OnMiniGameRequested += HandleMiniGameRequested;
            _rpc.OnMiniGameResultReceived += HandleMiniGameResultReceived;

            // 페이즈에 따른 플레이어 움직임 제어
            _rpc.CurrentPhase.Subscribe(OnPhaseChangedForMovement).AddTo(this);
            PlayerRegistry.OnPlayerRegistered += OnPlayerRegistered;

            Debug.Log($"[StageFlow] Initialize 완료 | 디버그모드={_debugMode} | 마스터={PhotonNetwork.IsMasterClient} | 환자수={_stageData.PatientCount} | 제한시간={_stageData.TotalTimeLimitSec}초");

            if (PhotonNetwork.IsMasterClient)
            {
                StartStageFlow().Forget();
            }
        }

        // ================================================================
        //  마스터 전용 - 메인 흐름
        // ================================================================

        private async UniTaskVoid StartStageFlow()
        {
            Debug.Log("[StageFlow] ========== 스테이지 플로우 시작 ==========");
            _flowCts = new CancellationTokenSource();
            var ct = _flowCts.Token;

            try
            {
                // Phase 1: 로딩 (집도의 선정 + 질병 생성)
                Debug.Log("[StageFlow] ▶ Phase 1: Loading 진입");
                _rpc.SetPhase(EStagePhase.Loading);
                await RunLoadingPhase(ct);
                Debug.Log("[StageFlow] ✓ Phase 1: Loading 완료");

                // Phase 2: 컷씬 (첫 환자 입장)
                Debug.Log("[StageFlow] ▶ Phase 2: Cutscene 진입");
                _rpc.SetPhase(EStagePhase.Cutscene);
                await RunCutscenePhase(ct);
                Debug.Log("[StageFlow] ✓ Phase 2: Cutscene 완료");

                // Phase 3: 게임 루프
                Debug.Log("[StageFlow] ▶ Phase 3: Playing 진입 (게임 루프 시작)");
                _rpc.SetPhase(EStagePhase.Playing);
                await RunGameLoop(ct);
                Debug.Log("[StageFlow] ✓ Phase 3: Playing 완료 (모든 환자 치료 성공)");

                // Phase 4: 스테이지 클리어
                Debug.Log("[StageFlow] ▶ Phase 4: StageClear! 5초 후 대기실 복귀");
                PausePatientHealthDrain();
                _timer.Pause();
                SyncTimerState();
                _rpc.SetPhase(EStagePhase.StageClear);
                OnStageClear?.Invoke();
                EventManager.Instance?.Publish(EventType.SurgerySuccess, "모든 환자 치료 완료!");

                await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: ct);
                Debug.Log("[StageFlow] 대기실로 복귀합니다.");
                PhotonServerManager.Instance.ReturnWaitingRoom();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StageFlow] 플로우 취소됨 (게임 오버)");
            }
        }

        // ── 로딩 페이즈 ─────────────────────────────────────────────

        private async UniTask RunLoadingPhase(CancellationToken ct)
        {
            // 1. 집도의 랜덤 선정 + ACK 대기
            Debug.Log("[StageFlow]   (1/3) 집도의 선정 중...");
            int surgeonActor = SelectSurgeon();
            await BroadcastAndWaitAck(
                () => _rpc.SetSurgeon(surgeonActor),
                handler => _rpc.OnSurgeonAckReceived += handler,
                handler => _rpc.OnSurgeonAckReceived -= handler,
                "집도의 선정",
                5000, ct);

            // 2. 질병 데이터 생성
            Debug.Log($"[StageFlow]   (2/3) 질병 데이터 생성 중... (환자 {_stageData.PatientCount}명)");
            await GenerateAllDiseases(ct);
            Debug.Log($"[StageFlow]   (2/3) 질병 데이터 생성 완료: {_stageData.Patients.Count}개");

            // 3. 스테이지 데이터를 클라이언트에 전송 + ACK 대기
            Debug.Log("[StageFlow]   (3/3) 스테이지 데이터 클라이언트 전송");
            string json = JsonUtility.ToJson(_stageData);
            await BroadcastAndWaitAck(
                () => _rpc.BroadcastStageData(json),
                handler => _rpc.OnStageDataAckReceived += handler,
                handler => _rpc.OnStageDataAckReceived -= handler,
                "스테이지 데이터",
                10000, ct);
        }

        /// <summary>
        /// 공용 ACK 대기 헬퍼. broadcast를 호출하고 모든 클라이언트의 ACK를 기다립니다.
        /// 타임아웃 시 retry를 1회 시도한 뒤 진행합니다.
        /// </summary>
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

                bool completed = await UniTask.WhenAny(
                    allAckTcs.Task,
                    UniTask.Delay(timeoutMs, cancellationToken: ct)
                ) == 0;

                if (completed)
                {
                    Debug.Log($"[StageFlow]   {label}: 모든 클라이언트 확인 완료");
                }
                else
                {
                    Debug.LogWarning($"[StageFlow]   {label}: ACK 타임아웃 ({pendingActors.Count}명 미응답) — 재전송");
                    broadcast();
                    await UniTask.Delay(2000, cancellationToken: ct);
                }
            }
            finally
            {
                unsubscribe(OnAck);
            }
        }

        private int SelectSurgeon()
        {
            Player[] players = PhotonNetwork.PlayerList;
            int randomIndex = UnityEngine.Random.Range(0, players.Length);
            int selectedActorNumber = players[randomIndex].ActorNumber;

            Debug.Log($"[StageFlow] 집도의 선정: Actor {selectedActorNumber}");
            return selectedActorNumber;
        }

        private async UniTask GenerateAllDiseases(CancellationToken ct)
        {
            _stageData.Patients.Clear();

            if (_debugMode || _diseaseGenManager == null)
            {
                List<DiseaseData> fallback = FallbackDiseaseLoader.GetRandom(_stageData.PatientCount);
                _stageData.Patients.AddRange(fallback);
                Debug.Log($"[StageFlow] 디버그 모드: Fallback 질병 {fallback.Count}개 로드");
                await UniTask.Yield(ct);
                return;
            }

            var tasks = new List<UniTask<DiseaseData>>();
            for (int i = 0; i < _stageData.PatientCount; i++)
            {
                tasks.Add(GenerateSingleDisease(ct));
            }

            DiseaseData[] diseases = await UniTask.WhenAll(tasks);

            for (int i = 0; i < diseases.Length; i++)
            {
                _stageData.Patients.Add(diseases[i]);
            }
        }

        private async UniTask<DiseaseData> GenerateSingleDisease(CancellationToken ct)
        {
            var result = await _diseaseGenManager.GenerateDisease(_stageData.Difficulty);
            ct.ThrowIfCancellationRequested();
            return result;
        }

        // ── 컷씬 페이즈 ────────────────────────────────────────────

        private async UniTask RunCutscenePhase(CancellationToken ct)
        {
            Debug.Log("[StageFlow]   컷씬 재생 시작 (3초 대기)");
            EventManager.Instance?.Publish(EventType.GameStart, "수술을 시작합니다!");

            // TODO: 실제 컷씬 시스템 연동 시 교체
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: ct);
            Debug.Log("[StageFlow]   컷씬 재생 종료");
        }

        // ── 게임 루프 ───────────────────────────────────────────────

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
                    await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: ct);
                    _rpc.SetPhase(EStagePhase.Playing);
                    _timer.Resume();
                    ResumePatientHealthDrain();
                    SyncTimerState();
                    Debug.Log("[StageFlow]   환자 전환 완료 (타이머 재개)");
                }

                await RunPatientLoop(i, ct);
            }
        }

        private async UniTask RunPatientLoop(int patientIndex, CancellationToken ct)
        {
            DiseaseData disease = _stageData.Patients[patientIndex];
            Debug.Log($"[StageFlow] ── 환자 {patientIndex + 1}/{_stageData.Patients.Count} 시작 | 병명: {disease.DiseaseName} | 레시피: {disease.Recipes?.Count ?? 0}단계 | 체력: {_stageData.InitialPatientHealth}");

            _judgeManager.SetDisease(disease);
            InitializePatientHealth();
            _emergencyHandler.ResetTimer();

            await RunRecipeLoop(disease, ct);

            Debug.Log($"[StageFlow] ── 환자 {patientIndex + 1}/{_stageData.Patients.Count} 치료 완료! | 병명: {disease.DiseaseName} | 남은 체력: {_rpc.PatientHealth.Value}");
        }

        private async UniTask RunRecipeLoop(DiseaseData disease, CancellationToken ct)
        {
            int recipeIndex = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                _rpc.SetRecipeIndex(recipeIndex);
                Debug.Log($"[StageFlow]     레시피 {recipeIndex + 1} 대기 중... (트레이 제출 대기)");

                SubmittedTray tray = await _trayHandler.WaitForSubmission(ct);
                Debug.Log("[StageFlow]     트레이 제출됨 → 판정 중...");

                TreatmentJudgeResult result = _judgeManager.JudgeNextRecipe(
                    tray, _rpc.PatientHealth.Value);

                if (result.Success)
                {
                    Debug.Log($"[StageFlow]     ✓ 레시피 {recipeIndex + 1} 성공! (ID: {result.CompletedRecipeId}) | 질병완치={result.DiseaseCured}");
                    EventManager.Instance?.Publish(
                        EventType.SurgerySuccess,
                        $"레시피 {result.CompletedRecipeId} 성공!");

                    // 집도의에게만 레시피 성공 후 수술 미니게임 권한을 부여합니다.
                    await RunRecipeMiniGame(ct);

                    if (result.DiseaseCured)
                    {
                        Debug.Log("[StageFlow]     ★ 질병 완치!");
                        return;
                    }

                    recipeIndex++;
                }
                else if (result.FailureReason == TreatmentFailureReason.RecipeMismatch)
                {
                    float newHealth = ApplyPatientDamage(disease.FailHealthPenalty);
                    Debug.Log($"[StageFlow]     ✗ 레시피 실패! 체력 -{disease.FailHealthPenalty} → 현재 체력: {newHealth}");

                    EventManager.Instance?.Publish(EventType.SurgeryFail, "잘못된 조합물!");

                    if (_isGameOver)
                    {
                        Debug.Log("[StageFlow]     !! 환자 사망 → 게임 오버");
                        return;
                    }

                    if (_emergencyHandler.ShouldTriggerOnRecipeFail())
                    {
                        Debug.Log("[StageFlow]     ⚡ 긴급 이벤트 발동!");
                        await HandleEmergencyEvent(ct);
                    }
                }
            }
        }

        // ── 트레이 제출 (외부 API — TraySubmissionHandler에 위임) ──

        public void OnTraySubmitted(SubmittedTray tray)
        {
            _trayHandler.OnTraySubmitted(tray);
        }

        public bool RequestTraySubmission(SubmittedTray tray, int trayViewId = -1)
        {
            return _trayHandler.RequestSubmission(tray, trayViewId);
        }

        // ── 타이머 동기화 ─────────────────────────────────────────────

        private void SyncTimerState()
        {
            _rpc.SetTimer(_timer.RemainingTime);
        }

        private void InitializePatientHealth()
        {
            if (_patientHealthController == null || _stageData == null)
            {
                return;
            }

            _patientHealthController.Initialize(
                _stageData.InitialPatientHealth,
                _stageData.PatientHealthDrainPerSecond);

            if (_rpc.CurrentPhase.Value == EStagePhase.Playing)
            {
                _patientHealthController.ResumeDrain();
            }
        }

        private float ApplyPatientDamage(float damage)
        {
            if (_patientHealthController == null)
            {
                return _rpc.PatientHealth.Value;
            }

            _patientHealthController.ApplyDamage(damage);
            return _rpc.PatientHealth.Value;
        }

        private void PausePatientHealthDrain()
        {
            _patientHealthController?.PauseDrain();
        }

        private void ResumePatientHealthDrain()
        {
            if (_rpc.CurrentPhase.Value == EStagePhase.Playing)
            {
                _patientHealthController?.ResumeDrain();
            }
        }

        // ── 레시피 성공 미니게임 ──────────────────────────────────────

        private async UniTask RunRecipeMiniGame(CancellationToken ct)
        {
            MiniGameType type = SelectRandomMiniGameType();
            int surgeonActorNumber = GetMiniGameTargetActorNumber();
            Debug.Log($"[StageFlow]     레시피 미니게임 시작: {type} | 집도의 Actor {surgeonActorNumber}");

            bool success;

            if (_debugMode)
            {
                Debug.Log("[StageFlow]     디버그: 미니게임 스킵");
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
                success = true;
            }
            else
            {
                success = await LaunchRoleMiniGame(surgeonActorNumber, type, ct);
            }

            Debug.Log($"[StageFlow]     레시피 미니게임 결과: {(success ? "성공" : "실패")}");

            if (!success)
            {
                float newHealth = ApplyPatientDamage(_miniGameFailPenalty);
                Debug.Log($"[StageFlow]     미니게임 실패! 체력 -{_miniGameFailPenalty} → 현재 체력: {newHealth}");

                if (_isGameOver)
                {
                    Debug.Log("[StageFlow]     !! 환자 사망 → 게임 오버");
                    return;
                }

                if (_emergencyHandler.ShouldTriggerOnRecipeFail())
                {
                    Debug.Log("[StageFlow]     ⚡ 미니게임 실패 → 긴급 이벤트 발동!");
                    await HandleEmergencyEvent(ct);
                }
            }
        }

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

            var tcs = new UniTaskCompletionSource<MiniGameResult>();
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                _miniGameLauncher.Launch(type, result => tcs.TrySetResult(result));
                MiniGameResult result = await tcs.Task;
                return result.IsSuccess;
            }
        }

        private async UniTask<bool> LaunchRoleMiniGame(int targetActorNumber, MiniGameType type, CancellationToken ct)
        {
            if (PhotonNetwork.LocalPlayer != null &&
                targetActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return await LaunchLocalMiniGame(type, ct);
            }

            return await LaunchRemoteMiniGame(targetActorNumber, type, ct);
        }

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
        /// </summary>
        private void HandleMiniGameResultReceived(bool success)
        {
            _miniGameResultTcs?.TrySetResult(success);
        }

        private MiniGameType SelectRandomMiniGameType()
        {
            MiniGameType[] types = (MiniGameType[])Enum.GetValues(typeof(MiniGameType));
            return types[UnityEngine.Random.Range(0, types.Length)];
        }

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

        private async UniTask HandleEmergencyEvent(CancellationToken ct)
        {
            EventManager.Instance?.Publish(EventType.PatientCritical, "긴급 처치가 필요합니다!");
            _rpc.BroadcastEmergency();

            float healthPenalty;

            if (_debugMode)
            {
                Debug.Log("[StageFlow] 디버그: 긴급 이벤트 발생 (미니게임 스킵)");
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
                healthPenalty = UnityEngine.Random.value > 0.5f ? 15f : 0f;
            }
            else
            {
                MiniGameType type = _emergencyHandler.GetRandomMiniGameType();
                int surgeonActorNumber = GetMiniGameTargetActorNumber();
                bool success = await LaunchRoleMiniGame(surgeonActorNumber, type, ct);
                healthPenalty = _emergencyHandler.GetPenaltyByMiniGameResult(success);
            }

            if (healthPenalty > 0f)
            {
                float newHealth = ApplyPatientDamage(healthPenalty);

                if (_isGameOver)
                {
                    Debug.Log($"[StageFlow]     긴급 처치 실패! 현재 체력: {newHealth}");
                }
            }
        }

        // ── 게임 오버 ───────────────────────────────────────────────

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

            string message = reason switch
            {
                EGameOverReason.PatientDeath => "환자가 사망했습니다...",
                EGameOverReason.TimeExpired => "제한 시간이 초과되었습니다!",
                _ => "게임 오버"
            };

            Debug.Log($"[StageFlow] {message} | 남은 타이머: {_timer.RemainingTime:F1}초 | 5초 후 대기실 복귀");
            EventManager.Instance?.Publish(EventType.GameOver, message);

            BroadcastGameOverAndWaitAck(reason).Forget();
        }

        private async UniTaskVoid BroadcastGameOverAndWaitAck(EGameOverReason reason)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(12000);

            try
            {
                await BroadcastAndWaitAck(
                    () => _rpc.BroadcastGameOver(reason),
                    handler => _rpc.OnGameOverAckReceived += handler,
                    handler => _rpc.OnGameOverAckReceived -= handler,
                    "게임 오버",
                    5000, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[StageFlow] 게임 오버 ACK 대기 중 타임아웃");
            }

            await ReturnToWaitingRoomDelayed();
        }

        private async UniTask ReturnToWaitingRoomDelayed()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5));
            PhotonServerManager.Instance.ReturnWaitingRoom();
        }

        // ── 플레이어 움직임 제어 ──────────────────────────────────────

        private void OnPhaseChangedForMovement(EStagePhase phase)
        {
            bool canMove = phase == EStagePhase.Playing;
            SetAllPlayersMovementLocked(!canMove);
        }

        private void OnPlayerRegistered(PlayerController player)
        {
            if (player != null && player.MovementAbility != null)
            {
                bool canMove = _rpc.CurrentPhase.Value == EStagePhase.Playing;
                player.MovementAbility.SetMovementLocked(!canMove);
            }
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

        // ── Update ──────────────────────────────────────────────────

        private void Update()
        {
            if (_debugMode && _debugAutoSubmit)
            {
                HandleDebugInput();
            }

            if (!PhotonNetwork.IsMasterClient || _isGameOver)
                return;

            if (_rpc.CurrentPhase.Value != EStagePhase.Playing)
                return;

            _patientHealthController?.Tick(Time.deltaTime);

            if (_isGameOver)
                return;

            if (_emergencyHandler.ShouldTriggerRandom(Time.deltaTime))
            {
                HandleEmergencyEvent(_flowCts.Token).Forget();
            }
        }

        // ── 디버그 ──────────────────────────────────────────────────

        private void HandleDebugInput()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (_rpc.CurrentPhase.Value != EStagePhase.Playing) return;
            if (!_trayHandler.IsWaitingForSubmission) return;

            if (Input.GetKeyDown(KeyCode.T))
            {
                SubmittedTray correctTray = CreateDebugCorrectTray();
                if (correctTray != null)
                {
                    Debug.Log("[StageFlow] 디버그: 정답 트레이 제출 (T)");
                    _trayHandler.OnTraySubmitted(correctTray);
                }
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                var wrongTray = new SubmittedTray();
                Debug.Log("[StageFlow] 디버그: 오답 트레이 제출 (Y)");
                _trayHandler.OnTraySubmitted(wrongTray);
            }
        }

        private SubmittedTray CreateDebugCorrectTray()
        {
            if (_judgeManager.CurrentDisease == null) return null;

            var completedIds = new List<string>(_judgeManager.CompletedRecipeIds);
            RecipeData nextRecipe = _judgeManager.CurrentDisease.GetNextRecipe(completedIds);
            if (nextRecipe == null) return null;

            var tray = new SubmittedTray();
            if (nextRecipe.RequiresSterilizedTray)
            {
                tray.SetTrayKind(TrayKind.Sterilized);
            }

            foreach (CraftedMaterialType material in nextRecipe.RequiredMaterials)
            {
                tray.TryAddItem(new CraftedItem
                {
                    MaterialType = material,
                    UsedToolsMask = ToolType.None,
                    UsedAction = ActionType.None,
                });
            }

            return tray;
        }

        // ── 정리 ────────────────────────────────────────────────────

        private void OnDestroy()
        {
            _flowCts?.Cancel();
            _flowCts?.Dispose();

            if (_onTimerExpired != null) _timer.OnExpired -= _onTimerExpired;
            if (_onTimerSyncTick != null) _timer.OnSyncTick -= _onTimerSyncTick;

            if (_rpc != null)
            {
                _rpc.OnMiniGameRequested -= HandleMiniGameRequested;
                _rpc.OnMiniGameResultReceived -= HandleMiniGameResultReceived;
            }

            PlayerRegistry.OnPlayerRegistered -= OnPlayerRegistered;

            if (_patientHealthController != null)
            {
                if (_onPatientHealthChanged != null) _patientHealthController.OnHealthChanged -= _onPatientHealthChanged;
                if (_onPatientHealthDepleted != null) _patientHealthController.OnHealthDepleted -= _onPatientHealthDepleted;
                _patientHealthController.Reset();
            }

            _trayHandler?.Dispose();
        }
    }
}
