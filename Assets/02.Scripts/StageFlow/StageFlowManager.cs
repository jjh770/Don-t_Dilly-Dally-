using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using DontDillyDally.MiniGame;
using Photon.Pun;
using Photon.Realtime;
using UniRx;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public class StageFlowManager : PunSingleton<StageFlowManager>
    {
        // ── 위임 컴포넌트 ────────────────────────────────────────────
        [Header("핸들러")]
        [SerializeField] private StageFlowRpcHandler _rpc;
        [SerializeField] private StageTimer _timer;
        [SerializeField] private EmergencyEventHandler _emergencyHandler;

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

        // ── 내부 상태 ────────────────────────────────────────────────
        private StageData _stageData;
        private TreatmentJudgeManager _judgeManager;
        private CancellationTokenSource _flowCts;
        private bool _isGameOver;

        // ── 외부 참조 (Bootstrapper에서 주입) ────────────────────────
        private DiseaseGenerationManager _diseaseGenManager;
        private MiniGameLauncher _miniGameLauncher;

        // ── 트레이 제출 브릿지 ───────────────────────────────────────
        private UniTaskCompletionSource<SubmittedTray> _traySubmissionTcs;

        // ── 캐시된 델리게이트 (구독 해제용) ─────────────────────────
        private Action _onTimerExpired;
        private Action<float> _onTimerSyncTick;

        // ── 이벤트 ──────────────────────────────────────────────────
        public event Action<EGameOverReason> OnGameOver;
        public event Action OnStageClear;

        // ================================================================
        //  초기화
        // ================================================================

        public void Initialize(
            DiseaseGenerationManager diseaseGenManager,
            MiniGameLauncher miniGameLauncher,
            StageData stageData)
        {
            _diseaseGenManager = diseaseGenManager;
            _miniGameLauncher = miniGameLauncher;
            _stageData = stageData;
            _judgeManager = new TreatmentJudgeManager();

            // 타이머 이벤트 바인딩
            _onTimerExpired = () => TriggerGameOver(EGameOverReason.TimeExpired);
            _onTimerSyncTick = time => _rpc.SetTimer(time);
            _timer.OnExpired += _onTimerExpired;
            _timer.OnSyncTick += _onTimerSyncTick;

            // 클라이언트 측 게임 오버 수신
            _rpc.OnGameOverReceived += reason => OnGameOver?.Invoke(reason);
            _rpc.OnStageDataReceived += data => _stageData = data;

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
            // 1. 집도의 랜덤 선정
            Debug.Log("[StageFlow]   (1/3) 집도의 선정 중...");
            SelectSurgeon();

            // 2. 질병 데이터 생성
            Debug.Log($"[StageFlow]   (2/3) 질병 데이터 생성 중... (환자 {_stageData.PatientCount}명)");
            await GenerateAllDiseases(ct);
            Debug.Log($"[StageFlow]   (2/3) 질병 데이터 생성 완료: {_stageData.Patients.Count}개");

            // 3. 스테이지 데이터를 클라이언트에 전송
            Debug.Log("[StageFlow]   (3/3) 스테이지 데이터 클라이언트 전송");
            string json = JsonUtility.ToJson(_stageData);
            _rpc.BroadcastStageData(json);
        }

        private void SelectSurgeon()
        {
            Player[] players = PhotonNetwork.PlayerList;
            int randomIndex = UnityEngine.Random.Range(0, players.Length);
            int selectedActorNumber = players[randomIndex].ActorNumber;

            _rpc.SetSurgeon(selectedActorNumber);
            Debug.Log($"[StageFlow] 집도의 선정: Actor {selectedActorNumber}");
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
            _rpc.SetTimer(_stageData.TotalTimeLimitSec);
            _timer.Resume();
            Debug.Log($"[StageFlow]   타이머 시작: {_stageData.TotalTimeLimitSec}초 | 환자 {_stageData.Patients.Count}명 치료 시작");

            for (int i = 0; i < _stageData.Patients.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                _rpc.SetPatientIndex(i);

                // 2번째 환자부터 전환 연출
                if (i > 0)
                {
                    Debug.Log("[StageFlow]   환자 전환 중... (타이머 일시정지)");
                    _timer.Pause();
                    _rpc.SetPhase(EStagePhase.PatientTransition);
                    await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: ct);
                    _rpc.SetPhase(EStagePhase.Playing);
                    _timer.Resume();
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
            _rpc.SetHealth(_stageData.InitialPatientHealth);
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

                SubmittedTray tray = await WaitForTraySubmission(ct);
                Debug.Log("[StageFlow]     트레이 제출됨 → 판정 중...");

                TreatmentJudgeResult result = _judgeManager.JudgeNextRecipe(
                    tray, _rpc.PatientHealth.Value);

                if (result.Success)
                {
                    Debug.Log($"[StageFlow]     ✓ 레시피 {recipeIndex + 1} 성공! (ID: {result.CompletedRecipeId}) | 질병완치={result.DiseaseCured}");
                    EventManager.Instance?.Publish(
                        EventType.SurgerySuccess,
                        $"레시피 {result.CompletedRecipeId} 성공!");

                    if (result.DiseaseCured)
                    {
                        Debug.Log("[StageFlow]     ★ 질병 완치!");
                        return;
                    }

                    recipeIndex++;
                }
                else if (result.FailureReason == TreatmentFailureReason.RecipeMismatch)
                {
                    float newHealth = _rpc.PatientHealth.Value - disease.FailHealthPenalty;
                    _rpc.SetHealth(newHealth);
                    Debug.Log($"[StageFlow]     ✗ 레시피 실패! 체력 -{disease.FailHealthPenalty} → 현재 체력: {newHealth}");

                    EventManager.Instance?.Publish(EventType.SurgeryFail, "잘못된 조합물!");

                    if (newHealth <= 0f)
                    {
                        Debug.Log("[StageFlow]     !! 환자 사망 → 게임 오버");
                        TriggerGameOver(EGameOverReason.PatientDeath);
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

        // ── 트레이 제출 ─────────────────────────────────────────────

        private async UniTask<SubmittedTray> WaitForTraySubmission(CancellationToken ct)
        {
            _traySubmissionTcs = new UniTaskCompletionSource<SubmittedTray>();

            using (ct.Register(() => _traySubmissionTcs.TrySetCanceled()))
            {
                return await _traySubmissionTcs.Task;
            }
        }

        /// <summary>
        /// 외부 시스템(트레이 상호작용)에서 호출합니다.
        /// 마스터 클라이언트에서만 처리됩니다.
        /// </summary>
        public void OnTraySubmitted(SubmittedTray tray)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            _traySubmissionTcs?.TrySetResult(tray);
        }

        // ── 긴급 이벤트 ─────────────────────────────────────────────

        private async UniTask HandleEmergencyEvent(CancellationToken ct)
        {
            EventManager.Instance?.Publish(EventType.PatientCritical, "긴급 처치가 필요합니다!");
            _rpc.BroadcastEmergency();

            float healthPenalty;

            if (_debugMode || _miniGameLauncher == null)
            {
                Debug.Log("[StageFlow] 디버그: 긴급 이벤트 발생 (미니게임 스킵)");
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
                healthPenalty = UnityEngine.Random.value > 0.5f ? 15f : 0f;
            }
            else
            {
                healthPenalty = await _emergencyHandler.ExecuteEmergency(
                    _miniGameLauncher, ct);
            }

            if (healthPenalty > 0f)
            {
                float newHealth = _rpc.PatientHealth.Value - healthPenalty;
                _rpc.SetHealth(newHealth);

                if (newHealth <= 0f)
                {
                    TriggerGameOver(EGameOverReason.PatientDeath);
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
            _timer.Pause();

            _rpc.SetPhase(EStagePhase.GameOver);
            _rpc.BroadcastGameOver(reason);

            OnGameOver?.Invoke(reason);

            string message = reason switch
            {
                EGameOverReason.PatientDeath => "환자가 사망했습니다...",
                EGameOverReason.TimeExpired => "제한 시간이 초과되었습니다!",
                _ => "게임 오버"
            };

            Debug.Log($"[StageFlow] {message} | 남은 타이머: {_timer.RemainingTime:F1}초 | 5초 후 대기실 복귀");
            EventManager.Instance?.Publish(EventType.GameOver, message);

            ReturnToWaitingRoomDelayed().Forget();
        }

        private async UniTaskVoid ReturnToWaitingRoomDelayed()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5));
            PhotonServerManager.Instance.ReturnWaitingRoom();
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
            if (_traySubmissionTcs == null) return;

            if (Input.GetKeyDown(KeyCode.T))
            {
                SubmittedTray correctTray = CreateDebugCorrectTray();
                if (correctTray != null)
                {
                    Debug.Log("[StageFlow] 디버그: 정답 트레이 제출 (T)");
                    OnTraySubmitted(correctTray);
                }
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                var wrongTray = new SubmittedTray();
                Debug.Log("[StageFlow] 디버그: 오답 트레이 제출 (Y)");
                OnTraySubmitted(wrongTray);
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
        }
    }
}
