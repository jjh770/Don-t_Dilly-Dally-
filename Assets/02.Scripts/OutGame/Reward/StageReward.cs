public readonly struct StageReward
{
    public int Stars { get; }
    public int Money { get; }
    public int MoneyDelta { get; }
    public bool IsNewBest { get; }
    public string SummaryText { get; }   
    public StageReward(int stars, int money, int moneyDelta, bool isNewBest, string summaryText = "")
    {
        Stars = stars;
        Money = money;
        MoneyDelta = moneyDelta;
        IsNewBest = isNewBest;
        SummaryText = summaryText ?? string.Empty;
    }
}
