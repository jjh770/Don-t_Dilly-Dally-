using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class AttendanceManager : MonoBehaviour
{
    private IAttendanceRepository _attendanceRepo;
    private AttendanceDomainService _domainService;
    private IRewardRepository _rewardRepo;
    private IAttendanceCheckPolicy _checkPolicy = new DailyAttendanceCheckPolicy();

    private bool _isChecking = false;
    public IRewardRepository RewardRepo => _rewardRepo;

    public event Action<AttendanceRecord> OnAttendanceRecordLoaded;
    public event Action<int> OnAttendanceChecked;

    public static event Action OnAttendanceManagerReady;

    public bool IsReady { get; private set; }

    public void Initialize(IAttendanceRepository attendanceRepo, IRewardRepository rewardRepo, IAttendanceCheckPolicy checkPolicy = null)
    {
        _attendanceRepo = attendanceRepo;
        _rewardRepo = rewardRepo;
        _rewardRepo = rewardRepo;
        _checkPolicy = checkPolicy ?? new DailyAttendanceCheckPolicy();

        _domainService = new AttendanceDomainService(_rewardRepo);

        OnAttendanceManagerReady?.Invoke();

        IsReady = true;
    }

    public void CheckAttendance(CancellationToken token)
    {
        if (_isChecking) return;

        _isChecking = true;
        CheckAttendanceAsync(token).Forget(Debug.LogException);
    }

    public void LoadAttendance(CancellationToken token)
    {
        LoadAttendanceAsync(token).Forget(Debug.LogException);
    }

    private async UniTask<AttendanceRecord> LoadAttendanceAsync(CancellationToken token)
    {
        var record = await LoadOrCreateRecordAsync(token);

        if (token.IsCancellationRequested) return null;

        OnAttendanceRecordLoaded?.Invoke(record);
        return record;
    }

    private async UniTask CheckAttendanceAsync(CancellationToken token)
    {
        try
        {
            var record = await LoadOrCreateRecordAsync(token);

            if (token.IsCancellationRequested) return;

            if (!record.CanCheckToday())
            {
                Debug.Log($"[AttendanceManager] {record.LastCheckedDate} : 이미 출석체크를 완료하였습니다.");
                return;
            }

            if (_domainService.RewardComplete(record))
            {
                Debug.Log("[AttendanceManager] 모든 보상을 수령 완료하였습니다.");
                return;
            }

            var reward = _domainService.CheckAndGetReward(record);
            Debug.Log($"{record.TotalDays} attendance checked: {reward.ItemId}");

            if (!string.IsNullOrEmpty(reward.ItemId) && CustomizingManager.Instance != null)
            {
                CustomizingManager.Instance.UnlockItem(reward.ItemId);
            }

            OnAttendanceChecked?.Invoke(record.TotalDays);

            await _attendanceRepo.SaveAsync(record)
                .AttachExternalCancellation(token);
        }
        finally
        {
            _isChecking = false;
        }
    }

    private async UniTask<AttendanceRecord> LoadOrCreateRecordAsync(CancellationToken token)
    {
        var record = await _attendanceRepo.LoadAsync()
            .AttachExternalCancellation(token);

        if (record == null)
        {
            record = new AttendanceRecord(0, null, _checkPolicy);
            Debug.Log("[AttendanceManager] Created a new attendance record.");
        }

        record.SetCheckPolicy(_checkPolicy);
        return record;
    }
}
