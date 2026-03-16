using System;

public enum EventPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public enum EventType
{
    // 긴급 상황 (사전 생성 음성 사용)
    GameStart,
    GameOver,
    SurgerySuccess,
    SurgeryFail,
    PatientDeath,

    // 일반 상황 (AI 실시간 생성)
    PatientCritical,
    PatientHealthDrop,
    AssistDeliverItem,
    MachineBroken,
    ChainAccident,
    PlayerMistake,
    TeamCooperation
}

[Serializable]
public class GameEvent
{
    public EventType Type { get; private set; }
    public EventPriority Priority { get; private set; }
    public string Description { get; private set; }
    public DateTime Timestamp { get; private set; }
    public bool UsePreGeneratedVoice { get; private set; }

    public GameEvent(EventType type, string description)
    {
        Type = type;
        Description = description;
        Timestamp = DateTime.Now;
        Priority = GetDefaultPriority(type);
        UsePreGeneratedVoice = IsPreGeneratedEvent(type);
    }

    public GameEvent(EventType type, string description, EventPriority priority)
    {
        Type = type;
        Description = description;
        Timestamp = DateTime.Now;
        Priority = priority;
        UsePreGeneratedVoice = IsPreGeneratedEvent(type);
    }

    private static EventPriority GetDefaultPriority(EventType type)
    {
        return type switch
        {
            EventType.PatientDeath => EventPriority.Critical,
            EventType.SurgeryFail => EventPriority.Critical,
            EventType.SurgerySuccess => EventPriority.Critical,
            EventType.GameStart => EventPriority.High,
            EventType.GameOver => EventPriority.High,
            EventType.PatientCritical => EventPriority.High,
            EventType.MachineBroken => EventPriority.High,
            EventType.ChainAccident => EventPriority.Normal,
            EventType.PlayerMistake => EventPriority.Normal,
            EventType.AssistDeliverItem => EventPriority.Low,
            EventType.TeamCooperation => EventPriority.Low,
            _ => EventPriority.Normal
        };
    }

    private static bool IsPreGeneratedEvent(EventType type)
    {
        return type switch
        {
            EventType.GameStart => true,
            EventType.GameOver => true,
            EventType.SurgerySuccess => true,
            EventType.SurgeryFail => true,
            EventType.PatientDeath => true,
            _ => false
        };
    }
}
