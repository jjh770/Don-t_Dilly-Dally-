using System;
using System.Diagnostics;

public class AttendanceDomainService
{
    private readonly IRewardRepository _rewardRepository;

    public AttendanceDomainService(IRewardRepository rewardRepository)
    {
        _rewardRepository = rewardRepository;
    }

    public bool RewardComplete(AttendanceRecord record)
    {
        return _rewardRepository.RewardComplete(record.TotalDays);
    }

    // 체크 + 보상 결정을 도메인이 모두 소유
    public AttendanceReward CheckAndGetReward(AttendanceRecord record)
    {
        record.MarkChecked();

        var reward = _rewardRepository.GetReward(record.TotalDays);
        return reward;
    }
}
