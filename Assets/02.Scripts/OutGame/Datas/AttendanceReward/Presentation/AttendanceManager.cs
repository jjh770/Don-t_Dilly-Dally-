using Cysharp.Threading.Tasks;
using UnityEngine;

public class AttendanceManager : MonoBehaviour
{
    private IAttendanceRepository _attendanceRepo;
    private IRewardRepository _rewardRepo;
    private AttendanceDomainService _domainService;
    private string _playerID;

    private bool _isCheckedToday = false;

    public void Initialize(IAttendanceRepository attendanceRepo, IRewardRepository rewardRepo, string playerID)
    {
        _attendanceRepo = attendanceRepo;
        _rewardRepo = rewardRepo;
        _playerID = playerID;

        _domainService = new AttendanceDomainService(rewardRepo);

        if (_isCheckedToday) return;

        CheckAttendanceAsync().Forget();
    }

    private async UniTask CheckAttendanceAsync()
    {
        var record = await _attendanceRepo.LoadAsync(_playerID);

        if (record == null)
        {
            record = new AttendanceRecord(_playerID);
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

        await _attendanceRepo.SaveAsync(_playerID, record);
       // PlayerDataManager.Instance.ApplyReward(reward);
        //_ui.ShowRewardPopup(reward);
    }
}
