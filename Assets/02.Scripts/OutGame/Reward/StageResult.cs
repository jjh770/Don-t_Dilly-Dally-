public readonly struct StageResult
{
    public int SavedCount { get; }
    public int PatientCount { get; }
    public int Difficulty { get; }
    public float SurvivalRatio => PatientCount > 0
        ? (float)SavedCount / PatientCount
        : 0f;

    public StageResult(int savedCount, int patientCount, int difficulty)
    {
        SavedCount = savedCount;
        PatientCount = patientCount;
        Difficulty = difficulty;
    }
}