using System.Collections.Generic;
using UnityEngine;

public sealed class RewardMoneyPolicy
{
    private const int MaxCoinBonus = 100;
    private const int MaxCoinPenalty = -50;

    public RewardMoneyAdjustment Evaluate(
        IReadOnlyList<StagePerformanceEvent> events,
        int currentMoney)
    {
        int requestedDelta = 0;

        foreach (StagePerformanceEvent e in events)
        {
            requestedDelta += e.EventType switch
            {
                EPerformanceEventType.TraySuccess => 5,
                EPerformanceEventType.TrayFail => -5,
                EPerformanceEventType.MiniGameSuccess => 10,
                EPerformanceEventType.MiniGameFail => -10,
                EPerformanceEventType.EmergencySuccess => 15,
                EPerformanceEventType.EmergencyFail => -15,
                EPerformanceEventType.PatientSaved => 20,
                EPerformanceEventType.PatientDied => -20,
                EPerformanceEventType.Timeout => -10,
                _ => 0
            };
        }

        requestedDelta = Mathf.Clamp(requestedDelta, MaxCoinPenalty, MaxCoinBonus);

        int appliedDelta = requestedDelta;
        if (appliedDelta < 0)
        {
            appliedDelta = -Mathf.Min(currentMoney, -appliedDelta);
        }

        return new RewardMoneyAdjustment(
            requestedDelta,
            appliedDelta,
            appliedDelta != requestedDelta);
    }
}
