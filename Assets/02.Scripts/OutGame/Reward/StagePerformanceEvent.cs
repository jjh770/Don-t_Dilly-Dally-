public readonly struct StagePerformanceEvent
{
    public string PlayerNickname { get; }
    public string PatientName { get; }
    public string DiseaseName { get; }
    public EPerformanceEventType EventType { get; }

    public StagePerformanceEvent(
        string playerNickname, string patientName, string diseaseName,
        EPerformanceEventType eventType)
    {
        PlayerNickname = playerNickname ?? string.Empty;
        PatientName = patientName ?? string.Empty;
        DiseaseName = diseaseName ?? string.Empty;
        EventType = eventType;
    }
}