public readonly struct RewardMoneyAdjustment
{
    public int RequestedDelta { get; }
    public int AppliedDelta { get; }
    public bool WasLimitedByBalance { get; }

    public RewardMoneyAdjustment(int requestedDelta, int appliedDelta, bool wasLimitedByBalance)
    {
        RequestedDelta = requestedDelta;
        AppliedDelta = appliedDelta;
        WasLimitedByBalance = wasLimitedByBalance;
    }
}
