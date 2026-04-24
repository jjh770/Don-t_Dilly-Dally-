using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PhotonView))]
public class BedInteractable : MonoBehaviourPun, IInteractable, IInRoomCallbacks, IOutlineTargetProvider
{
    private const int NoOccupantActorNumber = -1;
    private const int OfflineOccupantActorNumber = 1;
    private const int InvalidSlotIndex = -1;
    private const int DefaultPoseIndex = 0;
    private const int DefaultSlotCount = 2;

    [System.Serializable]
    public class BedSlot
    {
        [Tooltip("플레이어가 누울 위치/회전 기준")]
        public Transform LiePosition;

        [Tooltip("플레이어가 일어났을 때 이동할 위치. 미지정 시 누운 자리 유지.")]
        public Transform GetUpPosition;
    }

    [Header("침대 슬롯 (배열 순서대로 1층, 2층, ...)")]
    [SerializeField] private BedSlot[] _slots = new BedSlot[DefaultSlotCount];

    [Header("누운 포즈 개수 (애니메이터의 LyingPoseIndex 범위)")]
    [Min(1)]
    [SerializeField] private int _lyingPoseCount = 2;

    [Header("슬롯 인덱스와 동일한 포즈 사용 (꺼두면 랜덤)")]
    [SerializeField] private bool _poseFollowsSlotIndex = false;

    [Header("아웃라인을 그릴 대상 (메쉬가 있는 부모 오브젝트). 미지정 시 자기 자신.")]
    [SerializeField] private GameObject _outlineTarget;

    public GameObject OutlineTarget => _outlineTarget != null ? _outlineTarget : gameObject;

    public bool IsInteracting => AreAllSlotsOccupied();
    public Transform Transform => transform;

    private int[] _slotOccupantActorNumbers;
    private int[] _slotPoseIndices;
    private int _localSlotIndex = InvalidSlotIndex;
    private Transform _pendingInteractor;

    private Transform _localInteractor;
    private PlayerMovementAbility _localMovement;
    private PlayerAnimator _localAnimator;
    private Rigidbody _localRigidbody;
    private bool _cachedRigidbodyKinematic;

    private static bool IsInPhotonRoom => PhotonNetwork.InRoom;
    private int SlotCount => _slots != null ? _slots.Length : 0;

    private void Awake()
    {
        int slotCount = SlotCount;
        _slotOccupantActorNumbers = new int[slotCount];
        _slotPoseIndices = new int[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            _slotOccupantActorNumbers[i] = NoOccupantActorNumber;
            _slotPoseIndices[i] = DefaultPoseIndex;
        }
    }

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
        if (_localSlotIndex == InvalidSlotIndex)
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
        if (interactor == null)
        {
            return;
        }

        // 이미 점유 중이거나 요청 진행 중이면 중복 요청을 막는다.
        if (_localSlotIndex != InvalidSlotIndex || _pendingInteractor != null)
        {
            return;
        }

        if (!HasAnyValidSlotConfigured())
        {
            Debug.LogWarning($"[BedInteractable] '{name}'에 LiePosition이 설정된 슬롯이 없다.", this);
            return;
        }

        _pendingInteractor = interactor;

        if (!IsInPhotonRoom)
        {
            HandleOfflineOccupy(interactor);
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
        if (_localSlotIndex == InvalidSlotIndex)
        {
            return;
        }

        if (!IsInPhotonRoom)
        {
            int slotIndex = _localSlotIndex;
            _slotOccupantActorNumbers[slotIndex] = NoOccupantActorNumber;
            _slotPoseIndices[slotIndex] = DefaultPoseIndex;
            EndLocalOccupy(slotIndex);
            _localSlotIndex = InvalidSlotIndex;
            return;
        }

        int localActorNumber = GetLocalActorNumber();
        if (localActorNumber <= 0)
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
    private void RPC_ApplyOccupantState(int slotIndex, int actorNumber, int poseIndex, bool isOccupied)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
        {
            return;
        }

        _slotOccupantActorNumbers[slotIndex] = isOccupied ? actorNumber : NoOccupantActorNumber;
        _slotPoseIndices[slotIndex] = isOccupied ? poseIndex : DefaultPoseIndex;

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

            _localSlotIndex = slotIndex;
            BeginLocalOccupy(_pendingInteractor, _slots[slotIndex], poseIndex);
            _pendingInteractor = null;
        }
        else
        {
            int releasedSlot = _localSlotIndex;
            _localSlotIndex = InvalidSlotIndex;
            EndLocalOccupy(releasedSlot);
        }
    }

    [PunRPC]
    private void RPC_RejectOccupy(int actorNumber)
    {
        if (actorNumber != GetLocalActorNumber())
        {
            return;
        }

        _pendingInteractor = null;
    }

    private void TryGrantOccupy(int actorNumber)
    {
        // 마스터에서만 호출된다. 이미 이 플레이어가 어느 슬롯을 점유하고 있으면 중복 할당을 막는다.
        if (FindSlotIndexForActor(actorNumber) != InvalidSlotIndex)
        {
            return;
        }

        int emptySlot = FindEmptySlotIndex();
        if (emptySlot == InvalidSlotIndex)
        {
            SendRejectOccupy(actorNumber);
            return;
        }

        int poseIndex = PickPoseIndex(emptySlot);
        photonView.RPC(nameof(RPC_ApplyOccupantState), RpcTarget.AllViaServer, emptySlot, actorNumber, poseIndex, true);
    }

    private void ReleaseOccupy(int actorNumber)
    {
        int slotIndex = FindSlotIndexForActor(actorNumber);
        if (slotIndex == InvalidSlotIndex)
        {
            return;
        }

        photonView.RPC(nameof(RPC_ApplyOccupantState), RpcTarget.AllViaServer, slotIndex, actorNumber, DefaultPoseIndex, false);
    }

    private void SendRejectOccupy(int actorNumber)
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        Player requester = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (requester == null)
        {
            return;
        }

        photonView.RPC(nameof(RPC_RejectOccupy), requester, actorNumber);
    }

    private void HandleOfflineOccupy(Transform interactor)
    {
        int emptySlot = FindEmptySlotIndex();
        if (emptySlot == InvalidSlotIndex)
        {
            _pendingInteractor = null;
            return;
        }

        int poseIndex = PickPoseIndex(emptySlot);
        _slotOccupantActorNumbers[emptySlot] = OfflineOccupantActorNumber;
        _slotPoseIndices[emptySlot] = poseIndex;
        _localSlotIndex = emptySlot;

        BeginLocalOccupy(interactor, _slots[emptySlot], poseIndex);
        _pendingInteractor = null;
    }

    private void BeginLocalOccupy(Transform interactor, BedSlot slot, int poseIndex)
    {
        if (interactor == null || slot == null || slot.LiePosition == null)
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

        interactor.SetPositionAndRotation(slot.LiePosition.position, slot.LiePosition.rotation);

        if (_localMovement != null)
        {
            _localMovement.SetMovementLocked(this, true);
        }

        if (_localAnimator != null)
        {
            // 포즈 인덱스는 IsLying 전에 세팅해야 애니메이터 전이가 올바른 클립으로 진입한다.
            _localAnimator.SetLyingPoseIndex(poseIndex);
            _localAnimator.PlayLyingAnimation(true);
        }
    }

    private void EndLocalOccupy(int slotIndex)
    {
        if (_localAnimator != null)
        {
            _localAnimator.PlayLyingAnimation(false);
            _localAnimator.SetLyingPoseIndex(DefaultPoseIndex);
        }

        if (_localMovement != null)
        {
            _localMovement.SetMovementLocked(this, false);
        }

        if (_localRigidbody != null)
        {
            _localRigidbody.isKinematic = _cachedRigidbodyKinematic;
        }

        BedSlot slot = GetSlot(slotIndex);
        if (slot != null && slot.GetUpPosition != null && _localInteractor != null)
        {
            _localInteractor.SetPositionAndRotation(slot.GetUpPosition.position, slot.GetUpPosition.rotation);
        }

        _localInteractor = null;
        _localMovement = null;
        _localAnimator = null;
        _localRigidbody = null;
    }

    private void ReleaseLocalStateIfOccupying()
    {
        if (_localSlotIndex == InvalidSlotIndex)
        {
            return;
        }

        int slotIndex = _localSlotIndex;
        int localActorNumber = GetLocalActorNumber();

        if (IsInPhotonRoom && localActorNumber > 0 && _slotOccupantActorNumbers != null
            && slotIndex >= 0 && slotIndex < _slotOccupantActorNumbers.Length
            && _slotOccupantActorNumbers[slotIndex] == localActorNumber)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ReleaseOccupy(localActorNumber);
            }
            else if (photonView != null)
            {
                photonView.RPC(nameof(RPC_RequestVacate), RpcTarget.MasterClient, localActorNumber);
            }
        }

        _localSlotIndex = InvalidSlotIndex;
        EndLocalOccupy(slotIndex);

        if (!IsInPhotonRoom && _slotOccupantActorNumbers != null
            && slotIndex >= 0 && slotIndex < _slotOccupantActorNumbers.Length)
        {
            _slotOccupantActorNumbers[slotIndex] = NoOccupantActorNumber;
            _slotPoseIndices[slotIndex] = DefaultPoseIndex;
        }
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient || otherPlayer == null)
        {
            return;
        }

        int slotIndex = FindSlotIndexForActor(otherPlayer.ActorNumber);
        if (slotIndex == InvalidSlotIndex)
        {
            return;
        }

        ReleaseOccupy(otherPlayer.ActorNumber);
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 마스터만 신규 입장자에게 현재 점유 상태를 동기화한다.
        if (!PhotonNetwork.IsMasterClient || newPlayer == null || _slotOccupantActorNumbers == null)
        {
            return;
        }

        for (int i = 0; i < _slotOccupantActorNumbers.Length; i++)
        {
            if (_slotOccupantActorNumbers[i] > 0)
            {
                photonView.RPC(nameof(RPC_ApplyOccupantState), newPlayer, i,
                    _slotOccupantActorNumbers[i], _slotPoseIndices[i], true);
            }
        }
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

    private bool AreAllSlotsOccupied()
    {
        if (_slotOccupantActorNumbers == null || _slotOccupantActorNumbers.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < _slotOccupantActorNumbers.Length; i++)
        {
            if (_slotOccupantActorNumbers[i] <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private int FindEmptySlotIndex()
    {
        if (_slots == null)
        {
            return InvalidSlotIndex;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null || _slots[i].LiePosition == null)
            {
                continue;
            }

            if (_slotOccupantActorNumbers[i] <= 0)
            {
                return i;
            }
        }

        return InvalidSlotIndex;
    }

    private int FindSlotIndexForActor(int actorNumber)
    {
        if (_slotOccupantActorNumbers == null)
        {
            return InvalidSlotIndex;
        }

        for (int i = 0; i < _slotOccupantActorNumbers.Length; i++)
        {
            if (_slotOccupantActorNumbers[i] == actorNumber)
            {
                return i;
            }
        }

        return InvalidSlotIndex;
    }

    private int PickPoseIndex(int slotIndex)
    {
        int clampedCount = Mathf.Max(1, _lyingPoseCount);
        if (_poseFollowsSlotIndex)
        {
            return Mathf.Clamp(slotIndex, 0, clampedCount - 1);
        }

        return UnityEngine.Random.Range(0, clampedCount);
    }

    private bool HasAnyValidSlotConfigured()
    {
        if (_slots == null)
        {
            return false;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && _slots[i].LiePosition != null)
            {
                return true;
            }
        }

        return false;
    }

    private BedSlot GetSlot(int slotIndex)
    {
        if (_slots == null || slotIndex < 0 || slotIndex >= _slots.Length)
        {
            return null;
        }

        return _slots[slotIndex];
    }

    private static int GetLocalActorNumber()
    {
        return PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : NoOccupantActorNumber;
    }
}
