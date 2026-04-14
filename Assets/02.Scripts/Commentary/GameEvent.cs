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
    // 완전 고정형
    TimeOut,                // 타임아웃
    PatientDeath,           // 환자 사망

    // 템플릿형
    SurgerySuccess,         // 수술 성공 (협동)
    SurgeryFail,            // 수술 실패 (사고)
    NoSurgery,              // 수술을 특정 시간 동안 안 할 때 (사고)
    PatientCritical,        // 환자의 체력이 낮을 때 (사고)
    SuccessEmergencyEvent,  // 긴급 이벤트 성공 (협동)
    FailEmergencyEvent,     // 긴급 이벤트 실패 (사고)
    WrongMaterialUsed,      // 잘못된 재료 사용 (사고)

    // 완전 동적형
    NewPatientAppeared,     // 새로운 환자 등장
    ChainAccident,          // 사고가 이어질 때
    ChainCooperation        // 협동이 이어질 때
}

public enum EventCategory
{
    Neutral,        // 중립 (Chain 판정 제외)
    Cooperation,    // 협동
    Accident        // 사고
}

[Serializable]
public class GameEvent
{
    public EventType Type { get; private set; }
    public EventPriority Priority { get; private set; }
    public EventCategory Category { get; private set; }
    public string Description { get; private set; }
    public DateTime Timestamp { get; private set; }
    public bool UsePreGeneratedVoice { get; private set; }

    public GameEvent(EventType type, string description)
    {
        Type = type;
        Description = description;
        Timestamp = DateTime.Now;
        Priority = GetDefaultPriority(type);
        Category = GetCategory(type);
        UsePreGeneratedVoice = IsPreGeneratedEvent(type);
    }

    public GameEvent(EventType type, string description, EventPriority priority)
    {
        Type = type;
        Description = description;
        Timestamp = DateTime.Now;
        Priority = priority;
        Category = GetCategory(type);
        UsePreGeneratedVoice = IsPreGeneratedEvent(type);
    }

    private static EventCategory GetCategory(EventType type)
    {
        return type switch
        {
            // 협동
            EventType.SurgerySuccess => EventCategory.Cooperation,
            EventType.SuccessEmergencyEvent => EventCategory.Cooperation,

            // 사고
            EventType.SurgeryFail => EventCategory.Accident,
            EventType.NoSurgery => EventCategory.Accident,
            EventType.PatientCritical => EventCategory.Accident,
            EventType.FailEmergencyEvent => EventCategory.Accident,
            EventType.WrongMaterialUsed => EventCategory.Accident,

            // 중립 (Chain 판정 제외)
            _ => EventCategory.Neutral
        };
    }

    private static EventPriority GetDefaultPriority(EventType type)
    {
        return type switch
        {
            // 완전 고정형
            EventType.TimeOut => EventPriority.Critical,
            EventType.PatientDeath => EventPriority.Critical,

            // 템플릿형
            EventType.SurgerySuccess => EventPriority.Critical,
            EventType.SurgeryFail => EventPriority.Critical,
            EventType.NoSurgery => EventPriority.Normal,
            EventType.PatientCritical => EventPriority.Normal,
            EventType.SuccessEmergencyEvent => EventPriority.Normal,
            EventType.FailEmergencyEvent => EventPriority.Normal,
            EventType.WrongMaterialUsed => EventPriority.Normal,

            // 완전 동적형
            EventType.NewPatientAppeared => EventPriority.Critical,
            EventType.ChainAccident => EventPriority.Normal,
            EventType.ChainCooperation => EventPriority.Normal,

            _ => EventPriority.Normal
        };
    }

    // 사전 음성을 써야 하는지?
    private static bool IsPreGeneratedEvent(EventType type)
    {
        return type switch
        {
            EventType.TimeOut => true,
            EventType.PatientDeath => true,
            _ => false
        };
    }
}
