using UnityEngine;

public static class StageRewardCalculator
{
    // 별: 비율 기반 0~3
    public static int CalculateStars(StageResult result)
    {
        float ratio = result.SurvivalRatio;

        if (ratio <= 0f) return 0;
        if (ratio >= 1f) return 3;
        if (ratio > 0.5f) return 2;
        return 1;
    }

    // 돈: 1명 살릴 때마다 difficulty²에 비례
    // difficulty 1 → 10g, 2 → 40g, 3 → 90g (×10 × d²)
    public static int CalculateMoney(StageResult result)
    {
        int moneyPerPatient = 10 * result.Difficulty * result.Difficulty;
        return result.SavedCount * moneyPerPatient;
    }

    // 별은 최고 기록만 유지 (중복 누적 방지)
    public static StageReward Calculate(StageResult result, StageStars previousStars)
    {
        int earnedStars = CalculateStars(result);
        int earnedMoney = CalculateMoney(result);
        bool isNewBest = earnedStars > previousStars.Best;

        // 별은 이번 클리어에서 더 높은 경우만 갱신 (차등 지급도 가능, 아래 주석 참고)
        return new StageReward(earnedStars, earnedMoney, 0, isNewBest);
    }
}
