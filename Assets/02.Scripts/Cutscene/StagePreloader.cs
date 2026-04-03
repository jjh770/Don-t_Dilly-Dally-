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

    public StageRuntimeData StageData { get; private set; }
    public int SurgeonActorNumber { get; private set; } = -1;
    public bool IsRoleAssignmentComplete { get; private set; }
    public bool IsDataPrepComplete { get; private set; }

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
        IsRoleAssignmentComplete = true;

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
        IsRoleAssignmentComplete = true;
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
            // 1. 질병 데이터 생성
            Debug.Log($"[StagePreloader] (1/2) 질병 데이터 생성 중... (환자 {StageData.Settings.PatientSettings.PatientCount}명)");
            StageData.Patients.Clear();

            var tasks = new List<UniTask<DiseaseData>>();
            for (int i = 0; i < StageData.Settings.PatientSettings.PatientCount; i++)
            {
                tasks.Add(GenerateSingleDisease(ct));
            }

            DiseaseData[] diseases = await UniTask.WhenAll(tasks);
            foreach (var disease in diseases)
            {
                StageData.Patients.Add(disease);
            }
            Debug.Log($"[StagePreloader] (1/2) 질병 데이터 생성 완료: {StageData.Patients.Count}개");

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

    private async UniTask<DiseaseData> GenerateSingleDisease(CancellationToken ct)
    {
            var result = await _diseaseGenManager.GenerateDisease(StageData.Settings.PatientSettings.Difficulty);
        ct.ThrowIfCancellationRequested();
        return result;
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
