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
    GameStart,          // 게임 시작할 때
    GameOver,           // 게임 끝날 때
    SurgerySuccess,     // 수술에 성공했을 때
    SurgeryFail,        // 수술에 실패했을 때
    PatientDeath,       // 환자가 죽었을 때

    // 일반 상황 (AI 실시간 생성)
    PatientCritical,    // 환자가 응급 상황일 때
    PatientHealthDrop,  // 환자의 체력이 급하게 떨어질 때
    AssistDeliverItem,  // 어시스트가 아이템을 전달했을 때
    MachineBroken,      // 기계가 고장났을 때
    ChainAccident,      // 사고가 계속 이어질 때
    PlayerMistake,      // 플레이어가 실수했을 때
    TeamCooperation     // 팀이 협동할 때
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
