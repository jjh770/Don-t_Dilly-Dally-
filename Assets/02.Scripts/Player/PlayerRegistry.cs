using System;
using System.Collections.Generic;
using UnityEngine;

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

    public static bool TryGetLocalPlayer(out PlayerController player)
    {
        foreach (PlayerController candidate in Players.Values)
        {
            if (candidate == null)
            {
                continue;
            }

            if (candidate.PhotonView == null || candidate.PhotonView.IsMine)
            {
                player = candidate;
                return true;
            }
        }

        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController candidate in players)
        {
            if (candidate == null)
            {
                continue;
            }

            if (candidate.PhotonView == null || candidate.PhotonView.IsMine)
            {
                player = candidate;
                return true;
            }
        }

        player = null;
        return false;
    }

    public static bool TryGetLocalMovementAbility(out PlayerMovementAbility movementAbility)
    {
        movementAbility = null;

        if (!TryGetLocalPlayer(out PlayerController player) || player == null)
        {
            return false;
        }

        movementAbility = player.MovementAbility;
        return movementAbility != null;
    }

    public static IEnumerable<PlayerController> GetAllPlayers()
    {
        return Players.Values;
    }
}
