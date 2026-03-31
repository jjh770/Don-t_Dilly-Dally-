public interface IRewardRepository
{
    int GetRewardCount();

    AttendanceReward GetReward(int day);

    bool RewardComplete(int day);
}