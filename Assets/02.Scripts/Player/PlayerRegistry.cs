using System;
using System.Collections.Generic;

public static class PlayerRegistry
{
    private static readonly Dictionary<int, PlayerController> Players = new();

    public static event Action<PlayerController> OnPlayerRegistered;

    public static void Register(int actorNumber, PlayerController player)
    {
        if (player == null)
            return;

        Players[actorNumber] = player;
        OnPlayerRegistered?.Invoke(player);
    }

    public static void Unregister(int actorNumber, PlayerController player)
    {
        if (!Players.TryGetValue(actorNumber, out PlayerController current))
            return;

        if (current != player)
            return;

        Players.Remove(actorNumber);
    }

    public static bool TryGetPlayer(int actorNumber, out PlayerController player)
    {
        return Players.TryGetValue(actorNumber, out player);
    }

    public static IEnumerable<PlayerController> GetAllPlayers()
    {
        return Players.Values;
    }
}
