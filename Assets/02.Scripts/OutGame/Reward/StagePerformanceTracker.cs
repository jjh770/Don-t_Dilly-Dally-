using System.Collections.Generic;
using DontDillyDally.Data;
using Photon.Realtime;

public sealed class StagePerformanceTracker
{
    private readonly List<StagePerformanceEvent> _events = new();
    public IReadOnlyList<StagePerformanceEvent> Events => _events;

    public void Clear() => _events.Clear();

    public void Record(
        Player player,
        DiseaseData disease,
        EPerformanceEventType eventType)
    {
        _events.Add(new StagePerformanceEvent(
            player != null ? PlayerProperty.GetNickname(player) : string.Empty,
            disease?.PatientName ?? string.Empty,
            disease?.DiseaseName ?? string.Empty,
            eventType));
    }
}