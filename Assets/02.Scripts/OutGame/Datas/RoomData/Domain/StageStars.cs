using System;

public readonly struct StageStars
{
    public int Best { get; }

    public StageStars(int best)
    {
        Best = Math.Max(0, best);
    }

    public static StageStars Default => new(0);

    public StageStars KeepBest(int newStars) =>
        newStars > Best ? new(newStars) : this;
}