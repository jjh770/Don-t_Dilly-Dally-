public sealed class RewardSettlementService
{
    public StageReward Build(
        StageResult result,
        StageStars previousStars,
        RewardMoneyAdjustment adjustment,
        RewardNarrativeResult narrative)
    {
        StageReward baseReward = StageRewardCalculator.Calculate(result, previousStars);

        return new StageReward(
            baseReward.Stars,
            baseReward.Money + adjustment.AppliedDelta,
            adjustment.AppliedDelta,
            baseReward.IsNewBest,
            narrative?.summaryText ?? string.Empty);
    }
}
