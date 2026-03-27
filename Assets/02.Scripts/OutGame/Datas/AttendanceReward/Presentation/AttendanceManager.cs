using System;
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
    public void Initialize(IAttendanceRepository attendanceRepo, IRewardRepository rewardRepo, string playerID)
    {
        _attendanceRepo = attendanceRepo;
        _playerId = playerID;
        _rewardRepo = rewardRepo;

        _domainService = new AttendanceDomainService(_rewardRepo);
        OnAttendanceManagerReady?.Invoke();
        IsReady = true;
    }

    public void CheckAttendance()
    {
        if (_isCheckedToday) return;

        CheckAttendanceAsync().Forget();
    }

    public void LoadAttendance()
    {
        LoadAttendanceAsync().Forget();
    }

    private async UniTask<AttendanceRecord> LoadAttendanceAsync()
    {
        var record = await _attendanceRepo.LoadAsync(_playerId);

        if (record == null)
        {
            record = new AttendanceRecord(_playerId);
            Debug.Log($"[AttendanceManager] 새로운 데이터를 생성합니다.");
        }

        OnAttendanceRecordLoaded?.Invoke(record);
        return record;
    }

    private async UniTask CheckAttendanceAsync()
    {
        var record = await _attendanceRepo.LoadAsync(_playerId);

        if (record == null)
        {
            record = new AttendanceRecord(_playerId);
            Debug.Log($"[AttendanceManager] 새로운 데이터를 생성합니다.");
        }

        _isCheckedToday = true;

        if (!record.CanCheckToday())
        {
            //_ui.ShowAlreadyChecked();
            Debug.Log($"{record.TotalDays}일차 출석 이미 완료");
            return;
        }

        if (_domainService.RewardComplete(record))
        {
            // 모든 보상을 전부 수령
            Debug.Log($"모든 보상을 수령 완료");
            return;
        }
        var reward = _domainService.CheckAndGetReward(record);
        Debug.Log($"{record.TotalDays}일차 출석 : {reward.ItemId} 수령");
        OnAttendanceChecked?.Invoke(record.TotalDays);

        await _attendanceRepo.SaveAsync(_playerId, record);
    
       // PlayerDataManager.Instance.ApplyReward(reward);
        //_ui.ShowRewardPopup(reward);
    }
}
