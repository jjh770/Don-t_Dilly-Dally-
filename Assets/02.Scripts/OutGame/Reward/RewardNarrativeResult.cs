[System.Serializable]
public sealed class RewardNarrativeResult
{
    public const int InvalidMoneyDelta = int.MinValue;

    public string summaryText;
    public int requestedMoneyDelta = InvalidMoneyDelta;

    public bool HasRequestedMoneyDelta => requestedMoneyDelta != InvalidMoneyDelta;

    public static RewardNarrativeResult Fallback => new()
    {
        summaryText = "이번 수술 결과가 병원 기록에 반영되었습니다.",
        requestedMoneyDelta = InvalidMoneyDelta
    };
}
