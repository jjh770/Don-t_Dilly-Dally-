public readonly struct StageRewardSettlement
{
    public StageReward Reward { get; }
    public StageResult Result { get; }
    public int BeforeCoin { get; }
    public int AfterCoin { get; }
    public int BeforeStar { get; }
    public int AfterStar { get; }

    public StageRewardSettlement(
        StageReward reward,
        StageResult result,
        int beforeCoin,
        int afterCoin,
        int beforeStar,
        int afterStar)
    {
        Reward = reward;
        Result = result;
        BeforeCoin = beforeCoin;
        AfterCoin = afterCoin;
        BeforeStar = beforeStar;
        AfterStar = afterStar;
    }
}
