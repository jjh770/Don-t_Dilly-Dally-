using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class InfectionZone : MonoBehaviour
{
    [SerializeField] private float _speedMultiplier = 0.5f;

    private readonly HashSet<PlayerMovementAbility> _affectedPlayers = new();

    private void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent(out PhotonView photonView))
        {
            return;
        }

        if (!photonView.IsMine)
        {
            return;
        }

        if (!other.TryGetComponent(out PlayerMovementAbility movementAbility))
        {
            return;
        }

        if (_affectedPlayers.Contains(movementAbility))
        {
            return;
        }

        _affectedPlayers.Add(movementAbility);
        movementAbility.SetSpeedMultiplier(_speedMultiplier, 1f);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out PlayerMovementAbility movementAbility))
        {
            return;
        }

        if (_affectedPlayers.Remove(movementAbility))
        {
            movementAbility.SetSpeedMultiplier(1f, 1f);
        }
    }

    private void OnDestroy()
    {
        foreach (PlayerMovementAbility player in _affectedPlayers)
        {
            if (player != null)
            {
                player.SetSpeedMultiplier(1f, 1f);
            }
        }

        _affectedPlayers.Clear();
    }
}
