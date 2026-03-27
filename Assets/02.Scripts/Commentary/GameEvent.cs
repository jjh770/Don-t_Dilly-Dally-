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
    // - 일관성 있고
    // - 자주 나오고
    // - 즉시 필요한 것
    GameStart,              // 게임 시작할 때
    GameOver,               // 게임 끝날 때
    SurgerySuccess,         // 수술에 성공했을 때
    SurgeryFail,            // 수술에 실패했을 때
    PatientDeath,           // 환자가 죽었을 때

    // 템플릿형
    // - 여러 개 만들어서 랜덤 재생
    // - 매번 멘트가 같으면 심심한 것
    EquipmentAccident,      // 장비가 고장났을 때 (사고)
    PatientCritical,        // 환자가 응급 상황일 때 (피가 낮을 때)
    EmergencyEvent,         // 긴급 이벤트 발생
    MachineBroken,          // 기계가 고장났을 때

    // 완전 동적형
    // - 최근 이벤트의 문맥을 반영해야 자연스러운 것
    NewPatientAppeared,     // 새로운 환자가 등장했을 때
    MaterialDeliveredLate,  // 재료를 늦게 전달할 때 (사고)
    EmergencyPrevented,     // 긴급 이벤트를 막아냈을 때 (협동)
    WrongMaterialUsed,      // 잘못된 재료를 사용했을 때 (사고)
    RepairTimeout,          // 제한 시간 내에 장비를 고치지 못했을 때 (사고)
    RepairCompletedFast,    // 장비 수리를 빨리 했을 때 (협동)
    RepairCompletedLate,    // 장비 수리를 늦게 했을 때 (협동)
    ChainAccident,          // 사고가 계속 이어질 때
    ChainCooperation        // 협동이 계속 이어질 때
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
            // 완전 고정형
            EventType.GameStart => EventPriority.Critical,
            EventType.GameOver => EventPriority.Critical,
            EventType.SurgerySuccess => EventPriority.Critical,
            EventType.SurgeryFail => EventPriority.Critical,
            EventType.PatientDeath => EventPriority.Critical,

            // 템플릿형
            EventType.EquipmentAccident => EventPriority.High,
            EventType.PatientCritical => EventPriority.High,
            EventType.EmergencyEvent => EventPriority.High,
            EventType.MachineBroken => EventPriority.High,

            // 완전 동적형
            EventType.NewPatientAppeared => EventPriority.Critical,
            EventType.MaterialDeliveredLate => EventPriority.Normal,
            EventType.EmergencyPrevented => EventPriority.High,
            EventType.WrongMaterialUsed => EventPriority.High,
            EventType.RepairTimeout => EventPriority.High,
            EventType.RepairCompletedFast => EventPriority.Normal,
            EventType.RepairCompletedLate => EventPriority.Normal,
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
            EventType.GameStart => true,
            EventType.GameOver => true,
            EventType.SurgerySuccess => true,
            EventType.SurgeryFail => true,
            EventType.PatientDeath => true,
            _ => false
        };
    }
}
