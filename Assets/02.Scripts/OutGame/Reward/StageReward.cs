using UnityEngine;

public readonly struct StageReward
{
    public int Stars { get; }
    public int Money { get; }
    public bool IsNewBest { get; } // 이번 클리어가 최고 별 갱신 여부

    public StageReward(int stars, int money, bool isNewBest)
    {
        Stars = stars;
        Money = money;
        IsNewBest = isNewBest;
    }
}