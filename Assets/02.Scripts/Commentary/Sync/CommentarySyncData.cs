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
    public bool UsePreGeneratedVoice;
    public string PreGeneratedClipId;
    public string TtsAudioKey;
    public double ScheduledNetworkTime;
    public float EstimatedDuration;

    public CommentarySyncData() { }

    public CommentarySyncData(
        string commentaryId,
        int sequence,
        EventType eventType,
        EventPriority priority,
        string finalText,
        bool usePreGeneratedVoice,
        string preGeneratedClipId,
        string ttsAudioKey,
        double scheduledNetworkTime,
        float estimatedDuration)
    {
        CommentaryId = commentaryId;
        Sequence = sequence;
        EventType = eventType;
        Priority = priority;
        FinalText = finalText;
        UsePreGeneratedVoice = usePreGeneratedVoice;
        PreGeneratedClipId = preGeneratedClipId;
        TtsAudioKey = ttsAudioKey;
        ScheduledNetworkTime = scheduledNetworkTime;
        EstimatedDuration = estimatedDuration;
    }

    public static CommentarySyncData CreateFromEvent(
        GameEvent gameEvent,
        int sequence,
        string finalText,
        double scheduledNetworkTime,
        float estimatedDuration,
        string ttsAudioKey = null)
    {
        return new CommentarySyncData(
            commentaryId: Guid.NewGuid().ToString(),
            sequence: sequence,
            eventType: gameEvent.Type,
            priority: gameEvent.Priority,
            finalText: finalText,
            usePreGeneratedVoice: gameEvent.UsePreGeneratedVoice,
            preGeneratedClipId: gameEvent.UsePreGeneratedVoice ? gameEvent.Type.ToString() : null,
            ttsAudioKey: ttsAudioKey,
            scheduledNetworkTime: scheduledNetworkTime,
            estimatedDuration: estimatedDuration
        );
    }
}
