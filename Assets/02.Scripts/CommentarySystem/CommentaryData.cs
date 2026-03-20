using System;

/// <summary>
/// 네트워크로 전송할 코멘터리 데이터
/// </summary>
[Serializable]
public struct CommentaryData
{
    public int SequenceNumber;          // 중복/순서 검증용
    public int EventType;               // EventType enum
    public string NarrationText;        // 출력할 텍스트
    public bool UsePreGenerated;        // 사전 생성 음성 사용 여부
    public double NetworkPlayTime;      // PhotonNetwork.Time 기준 재생 시작 시각

    public CommentaryData(int sequence, EventType eventType, string text, bool preGenerated, double networkPlayTime)
    {
        SequenceNumber = sequence;
        EventType = (int)eventType;
        NarrationText = text;
        UsePreGenerated = preGenerated;
        NetworkPlayTime = networkPlayTime;
    }

    public EventType GetEventType() => (EventType)EventType;
}
