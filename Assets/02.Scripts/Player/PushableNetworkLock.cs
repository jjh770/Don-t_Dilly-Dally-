using DontDillyDally.StageFlow;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PushableItem))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PushableNetworkLock : MonoBehaviour, IPushable, IPunOwnershipCallbacks
{
    public bool IsInteracting => UsesNetworkLock
        ? _interactingActorNumber > 0 || _isPendingLocalInteract
        : _isOfflineInteracting;

    public Transform Transform => transform;

    private const int NoInteractorActorNumber = -1;

    private PushableItem _pushableItem;
    private PhotonView _photonView;
    private DiagnosisEmergencyMachine _diagnosisEmergencyMachine;
    private Transform _pendingLocalInteractor;
    private int _interactingActorNumber = NoInteractorActorNumber;
    private bool _isPendingLocalInteract;
    private bool _isOfflineInteracting;

    private bool UsesNetworkLock => PhotonNetwork.InRoom && _photonView != null;

    private void Awake()
    {
        _pushableItem = GetComponent<PushableItem>();
        TryGetComponent(out _photonView);
        TryGetComponent(out _diagnosisEmergencyMachine);
    }

    private void OnEnable()
    {
        if (_photonView != null)
        {
            PhotonNetwork.AddCallbackTarget(this);
        }
    }

    private void OnDisable()
    {
        if (_photonView != null)
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }
    }

    public void Interact(Transform interactor)
    {
        if (interactor == null)
        {
            return;
        }

        if (_diagnosisEmergencyMachine != null &&
            _diagnosisEmergencyMachine.TryHandleEmergencyInteract(interactor))
        {
            return;
        }

        if (!UsesNetworkLock)
        {
            if (_isOfflineInteracting)
            {
                return;
            }

            _pendingLocalInteractor = interactor;
            _isOfflineInteracting = true;
            _pushableItem.SetInteractionLocked(true);
            _pushableItem.BeginLocalPush(interactor);
            return;
        }

        int localActorNumber = GetLocalActorNumber();
        if (localActorNumber <= 0 || IsLockedByAnotherPlayer(localActorNumber) || IsControlledByLocalPlayer(localActorNumber))
        {
            return;
        }

        _pendingLocalInteractor = interactor;
        _isPendingLocalInteract = true;
        _pushableItem.SetInteractionLocked(true);

        if (PhotonNetwork.IsMasterClient)
        {
            TryGrantInteraction(localActorNumber);
            return;
        }

        _photonView.RPC(nameof(RPC_RequestInteract), RpcTarget.MasterClient, localActorNumber);
    }

    public void StopInteract()
    {
        if (!UsesNetworkLock)
        {
            _pendingLocalInteractor = null;
            _isOfflineInteracting = false;
            _pushableItem.EndLocalPush();
            _pushableItem.SetInteractionLocked(false);
            return;
        }

        int localActorNumber = GetLocalActorNumber();
        if (localActorNumber <= 0)
        {
            return;
        }

        _pendingLocalInteractor = null;
        _isPendingLocalInteract = false;
        _pushableItem.EndLocalPush();
        _pushableItem.SetInteractionLocked(false);

        RequestInteractionRelease(localActorNumber);
    }

    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if (targetView != _photonView || _photonView == null || _photonView.IsMine)
        {
            return;
        }

        int localActorNumber = GetLocalActorNumber();
        bool wasPreviousOwner = previousOwner != null && previousOwner.ActorNumber == localActorNumber;
        if (!_isPendingLocalInteract && !wasPreviousOwner && _interactingActorNumber != localActorNumber)
        {
            return;
        }

        ClearLocalInteractionState();
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
        if (targetView != _photonView || senderOfFailedRequest == null)
        {
            return;
        }

        if (senderOfFailedRequest.ActorNumber != GetLocalActorNumber())
        {
            return;
        }

        ClearLocalInteractionState();
    }

    [PunRPC]
    private void RPC_RequestInteract(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        TryGrantInteraction(actorNumber);
    }

    [PunRPC]
    private void RPC_RequestStopInteract(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        ReleaseInteraction(actorNumber);
    }

    [PunRPC]
    private void RPC_ApplyInteractState(int actorNumber, bool isInteracting)
    {
        if (!UsesNetworkLock)
        {
            return;
        }

        int localActorNumber = GetLocalActorNumber();
        _interactingActorNumber = isInteracting ? actorNumber : NoInteractorActorNumber;
        _pushableItem.SetInteractionLocked(isInteracting);

        if (!isInteracting)
        {
            ClearLocalInteractionState();
            return;
        }

        if (actorNumber == localActorNumber)
        {
            _isPendingLocalInteract = false;

            if (_pendingLocalInteractor != null)
            {
                _pushableItem.BeginLocalPush(_pendingLocalInteractor);
                return;
            }

            RequestInteractionRelease(localActorNumber);
            return;
        }

        ClearLocalInteractionState();
    }

    [PunRPC]
    private void RPC_RejectInteract(int actorNumber)
    {
        if (actorNumber != GetLocalActorNumber())
        {
            return;
        }

        ClearLocalInteractionState();
    }

    private void TryGrantInteraction(int actorNumber)
    {
        if (_photonView == null)
        {
            return;
        }

        if (_interactingActorNumber > 0 && _interactingActorNumber != actorNumber)
        {
            Player requester = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
            if (requester != null)
            {
                _photonView.RPC(nameof(RPC_RejectInteract), requester, actorNumber);
            }

            return;
        }

        _interactingActorNumber = actorNumber;
        _photonView.RPC(nameof(RPC_ApplyInteractState), RpcTarget.AllViaServer, actorNumber, true);

        if (_photonView.OwnerActorNr != actorNumber)
        {
            _photonView.TransferOwnership(actorNumber);
        }
    }

    private void ReleaseInteraction(int actorNumber)
    {
        if (_photonView == null || _interactingActorNumber != actorNumber)
        {
            return;
        }

        _interactingActorNumber = NoInteractorActorNumber;
        _photonView.RPC(nameof(RPC_ApplyInteractState), RpcTarget.AllViaServer, actorNumber, false);
    }

    private void ClearLocalInteractionState()
    {
        _pendingLocalInteractor = null;
        _isPendingLocalInteract = false;
        _pushableItem.EndLocalPush();
        _pushableItem.SetInteractionLocked(_interactingActorNumber > 0);
    }

    private void RequestInteractionRelease(int actorNumber)
    {
        if (_photonView == null)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            ReleaseInteraction(actorNumber);
        }
        else
        {
            _photonView.RPC(nameof(RPC_RequestStopInteract), RpcTarget.MasterClient, actorNumber);
        }

        if (PhotonNetwork.InRoom &&
            _photonView.IsMine &&
            _photonView.IsRoomView &&
            PhotonNetwork.MasterClient != null &&
            PhotonNetwork.MasterClient != PhotonNetwork.LocalPlayer)
        {
            _photonView.TransferOwnership(PhotonNetwork.MasterClient);
        }
    }

    private bool IsLockedByAnotherPlayer(int localActorNumber)
    {
        return _interactingActorNumber > 0 && _interactingActorNumber != localActorNumber;
    }

    private bool IsControlledByLocalPlayer(int localActorNumber)
    {
        return _interactingActorNumber == localActorNumber || _isPendingLocalInteract;
    }

    private static int GetLocalActorNumber()
    {
        return PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : NoInteractorActorNumber;
    }
}
