using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerRegistry
{
    private static readonly Dictionary<int, PlayerController> Players = new();
    private static PlayerController _localPlayer;

    public static event Action<PlayerController> OnPlayerRegistered;

    public static void Register(int actorNumber, PlayerController player)
    {
        if (player == null)
            return;

        Players[actorNumber] = player;
        CacheLocalPlayerIfNeeded(player);
        OnPlayerRegistered?.Invoke(player);
    }

    public static void RegisterLocal(PlayerController player)
    {
        if (player == null)
            return;

        _localPlayer = player;
        OnPlayerRegistered?.Invoke(player);
    }

    public static void Unregister(int actorNumber, PlayerController player)
    {
        if (!Players.TryGetValue(actorNumber, out PlayerController current))
            return;

        if (current != player)
            return;

        Players.Remove(actorNumber);
        ClearLocalPlayerIfMatched(player);
    }

    public static void UnregisterLocal(PlayerController player)
    {
        ClearLocalPlayerIfMatched(player);
    }

    public static bool TryGetPlayer(int actorNumber, out PlayerController player)
    {
        return Players.TryGetValue(actorNumber, out player);
    }

    public static bool TryGetLocalPlayer(out PlayerController player)
    {
        if (_localPlayer != null)
        {
            player = _localPlayer;
            return true;
        }

        player = null;
        return false;
    }

    public static IEnumerable<PlayerController> GetAllPlayers()
    {
        return Players.Values;
    }

    private static void CacheLocalPlayerIfNeeded(PlayerController player)
    {
        if (player == null || player.PhotonView == null || !player.PhotonView.IsMine)
        {
            return;
        }

        _localPlayer = player;
    }

    private static void ClearLocalPlayerIfMatched(PlayerController player)
    {
        if (_localPlayer == player)
        {
            _localPlayer = null;
        }
    }
}
