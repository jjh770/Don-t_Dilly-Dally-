namespace DontDillyDally.MiniGame
{
    public interface IMiniGame
    {
        MiniGameType GameType { get; }
        EMiniGameState CurrentState { get; }
        float NormalizedProgress { get; }

        void Begin(MiniGameConfig config);
        void Tick(float deltaTime);
        void Abort();

        event System.Action<MiniGameResult> OnCompleted;
    }
}
