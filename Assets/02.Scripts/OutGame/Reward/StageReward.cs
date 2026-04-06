public readonly struct StageReward
{
    public int Stars { get; }
    public int Money { get; }
    public bool IsNewBest { get; }
    public string SummaryText { get; }   
    public StageReward(int stars, int money, bool isNewBest, string summaryText = "")
    {
        Stars = stars;
        Money = money;
        IsNewBest = isNewBest;
        SummaryText = summaryText ?? string.Empty;
    }
}