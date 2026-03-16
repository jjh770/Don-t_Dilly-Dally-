namespace DontDillyDally.MiniGame
{
    public readonly struct QTEPrompt
    {
        public readonly Direction Direction;
        public readonly float TimeLimit;

        public QTEPrompt(Direction direction, float timeLimit)
        {
            Direction = direction;
            TimeLimit = timeLimit;
        }
    }
}
