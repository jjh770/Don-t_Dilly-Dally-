using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AttendanceManager : MonoBehaviour
{
    private IAttendanceRepository _attendanceRepo;
    private AttendanceDomainService _domainService;
    private IRewardRepository _rewardRepo;
    private string _playerId;

    private bool _isCheckedToday = false;
    public IRewardRepository RewardRepo => _rewardRepo;

    public event Action<AttendanceRecord> OnAttendanceRecordLoaded;
    public event Action<int> OnAttendanceChecked;

    public static event Action OnAttendanceManagerReady;

    public bool IsReady { get; private set; }

    // ✅ 추가
    private CancellationTokenSource _cts;

    public void Initialize(IAttendanceRepository attendanceRepo, IRewardRepository rewardRepo, string playerID)
    {
        _attendanceRepo = attendanceRepo;
        _playerId = playerID;
        _rewardRepo = rewardRepo;

        _domainService = new AttendanceDomainService(_rewardRepo);

        // ✅ 초기화 시 토큰 생성
        _cts = new CancellationTokenSource();

        OnAttendanceManagerReady?.Invoke();
  
        IsReady = true;
    }

    // ✅ 외부에서 호출 (Popup 닫힐 때)
    public void CancelAll()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource(); // 다시 쓸 수 있게 재생성
    }

    private void OnDestroy()
    {
        CancelAll();
    }

    public void CheckAttendance()
    {
        if (_isCheckedToday) return;

        CheckAttendanceAsync(_cts.Token).Forget(Debug.LogException);
    }

    public void LoadAttendance()
    {
        LoadAttendanceAsync(_cts.Token).Forget(Debug.LogException);
    }

    private async UniTask<AttendanceRecord> LoadAttendanceAsync(CancellationToken token)
    {
        var record = await _attendanceRepo.LoadAsync(_playerId)
            .AttachExternalCancellation(token);

        if (token.IsCancellationRequested) return null;

        if (record == null)
        {
            record = new AttendanceRecord(_playerId);
            Debug.Log($"[AttendanceManager] 새로운 데이터를 생성합니다.");
        }

        OnAttendanceRecordLoaded?.Invoke(record);
        return record;
    }

    private async UniTask CheckAttendanceAsync(CancellationToken token)
    {
        var record = await _attendanceRepo.LoadAsync(_playerId)
            .AttachExternalCancellation(token);

        if (token.IsCancellationRequested) return;

        if (record == null)
        {
            record = new AttendanceRecord(_playerId);
            Debug.Log($"[AttendanceManager] 새로운 데이터를 생성합니다.");
        }

        _isCheckedToday = true;

        if (!record.CanCheckToday())
        {
            Debug.Log($"{record.TotalDays}일차 출석 이미 완료");
            return;
        }

        if (_domainService.RewardComplete(record))
        {
            Debug.Log($"모든 보상을 수령 완료");
            return;
        }

        var reward = _domainService.CheckAndGetReward(record);
        Debug.Log($"{record.TotalDays}일차 출석 : {reward.ItemId} 수령");

        OnAttendanceChecked?.Invoke(record.TotalDays);

        await _attendanceRepo.SaveAsync(_playerId, record)
            .AttachExternalCancellation(token);
    }
}