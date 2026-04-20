using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class InfectionZone : MonoBehaviour
{
    [SerializeField] private float _speedMultiplier = 0.5f;
    [SerializeField] private float _debuffDuration = 5f;

    private readonly HashSet<int> _affectedPlayers = new();

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

        if (_affectedPlayers.Contains(photonView.ViewID))
        {
            return;
        }

        if (!other.TryGetComponent(out PlayerMovementAbility movementAbility))
        {
            return;
        }

        _affectedPlayers.Add(photonView.ViewID);
        movementAbility.ApplySpeedDebuff(_speedMultiplier, _debuffDuration);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out PhotonView photonView))
        {
            return;
        }

        _affectedPlayers.Remove(photonView.ViewID);
    }
}
