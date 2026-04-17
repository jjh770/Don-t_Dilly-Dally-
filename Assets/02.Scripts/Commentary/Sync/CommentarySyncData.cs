using System;

// 네트워크 전송용 코멘터리 데이터 DTO
[Serializable]
public class CommentarySyncData
{
    public string CommentaryId;
    public int Sequence;
    public EventType EventType;
    public EventPriority Priority;
    public string FinalText;
    public double ScheduledNetworkTime;
    public float EstimatedDuration;
    public bool IsDynamic;

    public CommentarySyncData() { }

    public CommentarySyncData(
        string commentaryId,
        int sequence,
        EventType eventType,
        EventPriority priority,
        string finalText,
        double scheduledNetworkTime,
        float estimatedDuration,
        bool isDynamic)
    {
        CommentaryId = commentaryId;
        Sequence = sequence;
        EventType = eventType;
        Priority = priority;
        FinalText = finalText;
        ScheduledNetworkTime = scheduledNetworkTime;
        EstimatedDuration = estimatedDuration;
        IsDynamic = isDynamic;
    }

    public static CommentarySyncData CreateFromEvent(
        GameEvent gameEvent,
        int sequence,
        string finalText,
        double scheduledNetworkTime,
        float estimatedDuration,
        bool isDynamic = false)
    {
        return new CommentarySyncData(
            commentaryId: Guid.NewGuid().ToString(),
            sequence: sequence,
            eventType: gameEvent.Type,
            priority: gameEvent.Priority,
            finalText: finalText,
            scheduledNetworkTime: scheduledNetworkTime,
            estimatedDuration: estimatedDuration,
            isDynamic: isDynamic
        );
    }
}
