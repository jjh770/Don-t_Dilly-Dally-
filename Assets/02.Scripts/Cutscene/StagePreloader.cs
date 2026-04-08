using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using DontDillyDally.StageFlow;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 컷씬 씬에서 질병/음성 데이터를 사전 생성하여 Gameplay 씬의 StageFlowManager에 전달합니다.
/// DontDestroyOnLoad로 씬 전환 간 데이터를 보존하며, PhotonView가 필요 없습니다.
/// </summary>
public class StagePreloader : MonoBehaviour
{
    public static StagePreloader Instance { get; private set; }

    [Header("병 정보 생성")]
    [SerializeField] private DiseaseGenerationManager _diseaseGenManager;

    [Header("생성 설정")]
    [Tooltip("이 시간(초) 내에 생성이 끝나지 않으면 나머지는 폴백 데이터로 채웁니다.")]
    [SerializeField] private float _totalGenerationTimeoutSec = 25f;

    [Tooltip("한 번의 API 호출로 생성할 최대 환자 수입니다.")]
    [SerializeField] private int _batchSize = 4;

    public StageRuntimeData StageData { get; private set; }
    public int SurgeonActorNumber { get; private set; } = -1;
    public bool IsRoleAssignmentComplete { get; private set; }
    public bool IsDataPrepComplete { get; private set; }
    public event Action<int> RoleAssignmentCompleted;

    private UniTaskCompletionSource _dataPrepTcs;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(StageRuntimeData stageData)
    {
        StageData = stageData;
        SurgeonActorNumber = -1;
        IsRoleAssignmentComplete = false;
        IsDataPrepComplete = false;
        _cts = new CancellationTokenSource();
        _dataPrepTcs = new UniTaskCompletionSource();
    }

    // ── 집도의 선정 ─────────────────────────────────────────────

    /// <summary>
    /// [MasterClient] 집도의를 선정합니다. SelectRoleManager가 자체 PhotonView로 RPC 전파합니다.
    /// </summary>
    public void AssignRoles()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        SurgeonActorNumber = SelectRoleManager.Instance?.AssignRoles() ?? -1;
        CompleteRoleAssignment();

        if (SurgeonActorNumber < 0)
        {
            Debug.LogError("[StagePreloader] 집도의 선정 실패");
            return;
        }

        Debug.Log($"[StagePreloader] ✓ 집도의 선정 완료: Actor {SurgeonActorNumber}");
    }

    /// <summary>
    /// 집도의가 선정될 때까지 대기합니다. (비마스터는 CustomProperties 동기화 대기)
    /// </summary>
    public async UniTask WaitForRoleAssignment(CancellationToken ct)
    {
        await UniTask.WaitUntil(() =>
        {
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (RoleProperties.GetPlayerRole(p) == RoleType.Surgeon)
                    return true;
            }
            return false;
        }, cancellationToken: ct);

        // 비마스터도 결과 저장
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (RoleProperties.GetPlayerRole(p) == RoleType.Surgeon)
            {
                SurgeonActorNumber = p.ActorNumber;
                break;
            }
        }
        CompleteRoleAssignment();
    }

    // ── 데이터 사전 생성 ─────────────────────────────────────────

    /// <summary>
    /// 컷씬과 병렬로 실행: 질병 생성 + 음성 사전 생성 (MasterClient 전용)
    /// </summary>
    public void StartDataPrep()
    {
        RunDataPrepAsync(_cts.Token).Forget();
    }

    /// <summary>
    /// 데이터 준비 완료까지 대기합니다.
    /// </summary>
    public async UniTask WaitForDataPrep(CancellationToken ct)
    {
        if (IsDataPrepComplete) return;
        await _dataPrepTcs.Task.AttachExternalCancellation(ct);
    }

    private async UniTaskVoid RunDataPrepAsync(CancellationToken ct)
    {
        try
        {
            // 1. 질병 데이터 병렬 생성 — 한 스테이지 환자 ≤5명 가정.
            //    Gemini Free Tier RPM 한도(15) 안에서 모두 동시에 쏘는 것이 가장 빠르다.
            //    1요청 ≈ 2~3초이므로 5요청 동시 발사 후 단일 웨이브로 ~3~5초 안에 종료.
            int patientCount = StageData.Settings.PatientSettings.PatientCount;
            int difficulty = StageData.Settings.PatientSettings.Difficulty;
            Debug.Log($"[StagePreloader] (1/2) 질병 데이터 병렬 생성 시작 (환자 {patientCount}명, 동시 발사)");
            StageData.Patients.Clear();

            float startTime = Time.realtimeSinceStartup;

            // 모든 환자 생성 작업을 한 번에 발사 (Semaphore 불필요 — 5건 << 15 RPM)
            var tasks = new UniTask<DiseaseData>[patientCount];
            for (int i = 0; i < patientCount; i++)
            {
                tasks[i] = GenerateOnePatientAsync(difficulty, StageData.StageId, ct);
            }

            DiseaseData[] results = await UniTask.WhenAll(tasks);

            // 결과를 순서대로 등록 — null이면 폴백으로 즉시 대체
            for (int i = 0; i < results.Length; i++)
            {
                DiseaseData disease = results[i];
                if (disease == null)
                {
                    disease = FallbackDiseaseLoader.GetRandom(StageData.StageId);
                    Debug.LogWarning($"[StagePreloader] 환자 {i + 1}/{patientCount} AI 실패 → 폴백 사용: {disease.DiseaseName}");
                }
                else
                {
                    Debug.Log($"[StagePreloader] 환자 {i + 1}/{patientCount}: {disease.DiseaseName} (출처: {disease.Source})");
                }
                StageData.Patients.Add(disease);
            }

            Debug.Log($"[StagePreloader] (1/2) 질병 데이터 생성 완료: {StageData.Patients.Count}개 (소요: {Time.realtimeSinceStartup - startTime:F1}초)");

            // 2. 환자 소개 음성 사전 생성
            Debug.Log("[StagePreloader] (2/2) 환자 소개 음성 사전 생성 중...");
            if (CommentaryController.Instance != null)
            {
                var infos = new List<(string, string)>();
                foreach (var patient in StageData.Patients)
                {
                    infos.Add((patient.PatientName, patient.DiseaseName));
                }
                await CommentaryController.Instance.PreGeneratePatientIntros(infos, ct);
            }
            Debug.Log("[StagePreloader] (2/2) 환자 소개 음성 사전 생성 완료");

            IsDataPrepComplete = true;
            _dataPrepTcs.TrySetResult();
            Debug.Log("[StagePreloader] ✓ 데이터 준비 완료");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[StagePreloader] 데이터 준비 취소됨");
        }
    }

    /// <summary>
    /// 단일 환자 1명을 생성합니다. 예외는 내부에서 흡수하고 실패 시 null을 반환합니다.
    /// (호출부에서 null이면 폴백으로 대체)
    /// </summary>
    private async UniTask<DiseaseData> GenerateOnePatientAsync(int difficulty, string stageId, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            DiseaseData disease = await _diseaseGenManager.GenerateDisease(difficulty, null, stageId);
            return disease;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[StagePreloader] 개별 환자 생성 예외: {e.Message}");
            return null;
        }
    }

    private void CompleteRoleAssignment()
    {
        IsRoleAssignmentComplete = true;
        RoleAssignmentCompleted?.Invoke(SurgeonActorNumber);
    }

    public void Cleanup()
    {
        DisposeCts();
        if (Instance == this) Instance = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        DisposeCts();
        if (Instance == this) Instance = null;
    }

    private void DisposeCts()
    {
        if (_cts == null) return;
        try { _cts.Cancel(); } catch (ObjectDisposedException) { }
        _cts.Dispose();
        _cts = null;
    }
}
