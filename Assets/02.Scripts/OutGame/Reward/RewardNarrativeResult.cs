[System.Serializable]
public sealed class RewardNarrativeResult
{
    public string summaryText;

    public static RewardNarrativeResult Fallback => new()
    {
        summaryText = "이번 수술 결과가 병원 기록에 반영되었습니다."
    };
}
