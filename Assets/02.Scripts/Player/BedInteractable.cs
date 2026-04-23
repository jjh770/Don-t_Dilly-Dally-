using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PhotonView))]
public class BedInteractable : MonoBehaviourPun, IInteractable, IInRoomCallbacks
{
    private const int NoOccupantActorNumber = -1;
    private const int OfflineOccupantActorNumber = 1;

    [Header("누운 위치 / 회전 기준")]
    [SerializeField] private Transform _liePosition;

    [Header("일어날 위치 (선택, 미지정 시 현재 위치 유지)")]
    [SerializeField] private Transform _getUpPosition;

    public bool IsInteracting => _occupyingActorNumber > 0;
    public Transform Transform => transform;

    private int _occupyingActorNumber = NoOccupantActorNumber;
    private Transform _pendingInteractor;
    private Transform _localInteractor;
    private PlayerMovementAbility _localMovement;
    private PlayerAnimator _localAnimator;
    private Rigidbody _localRigidbody;
    private bool _cachedRigidbodyKinematic;

    private static bool IsInPhotonRoom => PhotonNetwork.InRoom;

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        ReleaseLocalStateIfOccupying();
    }

    private void Update()
    {
        if (_localInteractor == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            StopInteract();
        }
    }

    public void Interact(Transform interactor)
    {
        if (interactor == null || _liePosition == null)
        {
            return;
        }

        if (IsInteracting)
        {
            return;
        }

        _pendingInteractor = interactor;

        if (!IsInPhotonRoom)
        {
            _occupyingActorNumber = OfflineOccupantActorNumber;
            BeginLocalOccupy(interactor);
            _pendingInteractor = null;
            return;
        }

        int localActorNumber = GetLocalActorNumber();
        if (localActorNumber <= 0)
        {
            _pendingInteractor = null;
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            TryGrantOccupy(localActorNumber);
            return;
        }

        photonView.RPC(nameof(RPC_RequestOccupy), RpcTarget.MasterClient, localActorNumber);
    }

    public void StopInteract()
    {
        if (!IsInPhotonRoom)
        {
            EndLocalOccupy();
            _occupyingActorNumber = NoOccupantActorNumber;
            return;
        }

        int localActorNumber = GetLocalActorNumber();
        if (localActorNumber <= 0 || _occupyingActorNumber != localActorNumber)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            ReleaseOccupy(localActorNumber);
            return;
        }

        photonView.RPC(nameof(RPC_RequestVacate), RpcTarget.MasterClient, localActorNumber);
    }

    [PunRPC]
    private void RPC_RequestOccupy(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        TryGrantOccupy(actorNumber);
    }

    [PunRPC]
    private void RPC_RequestVacate(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        ReleaseOccupy(actorNumber);
    }

    [PunRPC]
    private void RPC_ApplyOccupantState(int actorNumber, bool isOccupied)
    {
        _occupyingActorNumber = isOccupied ? actorNumber : NoOccupantActorNumber;

        int localActorNumber = GetLocalActorNumber();
        if (actorNumber != localActorNumber)
        {
            return;
        }

        if (isOccupied)
        {
            if (_pendingInteractor == null)
            {
                return;
            }

            BeginLocalOccupy(_pendingInteractor);
            _pendingInteractor = null;
        }
        else
        {
            EndLocalOccupy();
        }
    }

    private void TryGrantOccupy(int actorNumber)
    {
        if (IsInteracting && _occupyingActorNumber != actorNumber)
        {
            return;
        }

        photonView.RPC(nameof(RPC_ApplyOccupantState), RpcTarget.AllViaServer, actorNumber, true);
    }

    private void ReleaseOccupy(int actorNumber)
    {
        if (_occupyingActorNumber != actorNumber)
        {
            return;
        }

        photonView.RPC(nameof(RPC_ApplyOccupantState), RpcTarget.AllViaServer, actorNumber, false);
    }

    private void BeginLocalOccupy(Transform interactor)
    {
        if (interactor == null || _liePosition == null)
        {
            return;
        }

        // 중복 진입 시 저장된 kinematic 원본값이 덮어써지는 것을 방지한다.
        if (_localInteractor != null)
        {
            return;
        }

        _localInteractor = interactor;
        _localMovement = interactor.GetComponent<PlayerMovementAbility>();
        _localAnimator = interactor.GetComponent<PlayerAnimator>();
        _localRigidbody = interactor.GetComponent<Rigidbody>();

        if (_localRigidbody != null)
        {
            _cachedRigidbodyKinematic = _localRigidbody.isKinematic;
            _localRigidbody.linearVelocity = Vector3.zero;
            _localRigidbody.angularVelocity = Vector3.zero;
            _localRigidbody.isKinematic = true;
        }

        interactor.SetPositionAndRotation(_liePosition.position, _liePosition.rotation);

        if (_localMovement != null)
        {
            _localMovement.SetMovementLocked(this, true);
        }

        if (_localAnimator != null)
        {
            _localAnimator.PlayLyingAnimation(true);
        }
    }

    private void EndLocalOccupy()
    {
        if (_localAnimator != null)
        {
            _localAnimator.PlayLyingAnimation(false);
        }

        if (_localMovement != null)
        {
            _localMovement.SetMovementLocked(this, false);
        }

        if (_localRigidbody != null)
        {
            _localRigidbody.isKinematic = _cachedRigidbodyKinematic;
        }

        if (_getUpPosition != null && _localInteractor != null)
        {
            _localInteractor.SetPositionAndRotation(_getUpPosition.position, _getUpPosition.rotation);
        }

        _localInteractor = null;
        _localMovement = null;
        _localAnimator = null;
        _localRigidbody = null;
    }

    private void ReleaseLocalStateIfOccupying()
    {
        if (_localInteractor == null)
        {
            return;
        }

        // 네트워크 상태 해제 RPC는 룸 유지 중이고 로컬이 점유자일 때만 전송한다.
        if (IsInPhotonRoom && _occupyingActorNumber == GetLocalActorNumber())
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ReleaseOccupy(_occupyingActorNumber);
            }
            else if (photonView != null)
            {
                photonView.RPC(nameof(RPC_RequestVacate), RpcTarget.MasterClient, _occupyingActorNumber);
            }
        }

        EndLocalOccupy();

        if (!IsInPhotonRoom)
        {
            _occupyingActorNumber = NoOccupantActorNumber;
        }
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient || otherPlayer == null)
        {
            return;
        }

        if (_occupyingActorNumber != otherPlayer.ActorNumber)
        {
            return;
        }

        ReleaseOccupy(otherPlayer.ActorNumber);
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 뒤늦게 입장한 플레이어에게 현재 점유 상태를 동기화한다.
        if (!PhotonNetwork.IsMasterClient || !IsInteracting)
        {
            return;
        }

        photonView.RPC(nameof(RPC_ApplyOccupantState), newPlayer, _occupyingActorNumber, true);
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
    }

    private static int GetLocalActorNumber()
    {
        return PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : NoOccupantActorNumber;
    }
}
