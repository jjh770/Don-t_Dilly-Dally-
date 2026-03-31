public interface IRewardRepository
{
    int GetRewardCount();

    AttendanceReward GetReward(int day);

    bool RewardComplete(int day);

    bool IsRewardItem(string itemId);
}