using System;

/// <summary>
/// 네트워크 전송용 코멘터리 데이터 DTO
/// 호스트가 확정한 코멘터리 정보를 클라이언트에게 전송할 때 사용
/// </summary>
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

    public CommentarySyncData() { }

    public CommentarySyncData(
        string commentaryId,
        int sequence,
        EventType eventType,
        EventPriority priority,
        string finalText,
        double scheduledNetworkTime,
        float estimatedDuration)
    {
        CommentaryId = commentaryId;
        Sequence = sequence;
        EventType = eventType;
        Priority = priority;
        FinalText = finalText;
        ScheduledNetworkTime = scheduledNetworkTime;
        EstimatedDuration = estimatedDuration;
    }

    public static CommentarySyncData CreateFromEvent(
        GameEvent gameEvent,
        int sequence,
        string finalText,
        double scheduledNetworkTime,
        float estimatedDuration)
    {
        return new CommentarySyncData(
            commentaryId: Guid.NewGuid().ToString(),
            sequence: sequence,
            eventType: gameEvent.Type,
            priority: gameEvent.Priority,
            finalText: finalText,
            scheduledNetworkTime: scheduledNetworkTime,
            estimatedDuration: estimatedDuration
        );
    }
}
