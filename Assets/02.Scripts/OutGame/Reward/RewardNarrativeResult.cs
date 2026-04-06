[System.Serializable]
public sealed class RewardNarrativeResult
{
    public string summaryText;
    public int coinDelta;

    // LLM 실패 시 기본값
    public static RewardNarrativeResult Fallback => new()
    {
        summaryText = "오늘의 수술이 마무리되었습니다.",
        coinDelta = 0
    };
}