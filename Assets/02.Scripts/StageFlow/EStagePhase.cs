namespace DontDillyDally.StageFlow
{
    public enum EStagePhase
    {
        None = 0,
        Loading = 10,
        Cutscene = 20,
        Countdown = 25,
        Playing = 30,
        PatientTransition = 40,
        StageClear = 50,
        GameOver = 60
    }

    public enum EGameOverReason
    {
        None = 0,
        PatientDeath = 1,
        TimeExpired = 2
    }
}
