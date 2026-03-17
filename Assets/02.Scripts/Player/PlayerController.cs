using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PlayerMovementAbility))]
public class PlayerController : MonoBehaviour
{
    private PhotonView _photonView;

    public PhotonView PhotonView => _photonView;
    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }
}
