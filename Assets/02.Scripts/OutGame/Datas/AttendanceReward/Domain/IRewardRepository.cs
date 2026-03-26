public interface IRewardRepository
{
    AttendanceReward GetReward(int day);

    bool RewardComplete(int day);
}