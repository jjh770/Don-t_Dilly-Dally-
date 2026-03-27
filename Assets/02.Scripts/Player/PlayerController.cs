using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PlayerMovementAbility))]
public class PlayerController : MonoBehaviour
{
    private PhotonView _photonView;
    private PlayerHeldItemController _heldItemController;
    private PlayerMovementAbility _movementAbility;

    public PhotonView PhotonView => _photonView;
    public PlayerMovementAbility MovementAbility => _movementAbility;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _heldItemController = GetComponent<PlayerHeldItemController>();
        _movementAbility = GetComponent<PlayerMovementAbility>();
    }

    private void OnEnable()
    {
        RegisterSelf();
    }

    private void Start()
    {
        RegisterSelf();
    }

    private void OnDisable()
    {
        if (_photonView?.Owner == null)
            return;

        PlayerRegistry.Unregister(_photonView.Owner.ActorNumber, this);
    }

    public Transform GetHoldPoint()
    {
        return _heldItemController != null ? _heldItemController.HoldPoint : null;
    }

    private void RegisterSelf()
    {
        if (_photonView?.Owner == null)
            return;

        PlayerRegistry.Register(_photonView.Owner.ActorNumber, this);
    }
}
