using DontDillyDally.StageFlow;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerSpawnManager : PunSingleton<PlayerSpawnManager>
{
    private struct SpawnSelection
    {
        public bool UseSurgeonSpawnPoint;
        public int Index;

        public SpawnSelection(bool useSurgeonSpawnPoint, int index)
        {
            UseSurgeonSpawnPoint = useSurgeonSpawnPoint;
            Index = index;
        }
    }

    [Header("Spawn Mode")]
    [SerializeField] private bool _useSpawnArrange;
    [Tooltip("UseSpawnArrange 모드에서 사용할 스폰 영역 (로비 등)")]
    [SerializeField] private Collider _spawnArea;

    [Header("Player")]
    [SerializeField] private GameObject _playerPrefab;

    private readonly Dictionary<int, int> _usedSpawnPoints = new Dictionary<int, int>();

    private GameObject _player;
    private bool _hasSpawnedLocalPlayer;
    private bool _isSpawnRequestPending;
    private int _surgeonSpawnActorNumber = -1;

    public event Action<GameObject> OnPlayerSpawned;

    public override void OnJoinedRoom()
    {
        SubscribeToRoleAssignment();
        TrySpawnLocalPlayer();
    }

    public override void OnLeftRoom()
    {
        UnsubscribeFromRoleAssignment();
        CleanupExistingLocalPlayerObject();
        _usedSpawnPoints.Clear();
        _surgeonSpawnActorNumber = -1;
        _player = null;
        _hasSpawnedLocalPlayer = false;
        _isSpawnRequestPending = false;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer == null)
        {
            return;
        }

        _usedSpawnPoints.Remove(otherPlayer.ActorNumber);

        if (_surgeonSpawnActorNumber == otherPlayer.ActorNumber)
        {
            _surgeonSpawnActorNumber = -1;
        }
    }

    private void Start()
    {
        TrySpawnLocalPlayer();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SubscribeToRoleAssignment();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeFromRoleAssignment();
    }

    private void TrySpawnLocalPlayer()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        if (_hasSpawnedLocalPlayer && _player != null)
        {
            return;
        }

        if (_hasSpawnedLocalPlayer && _player == null)
        {
            _hasSpawnedLocalPlayer = false;
        }

        CleanupExistingLocalPlayerObject();

        if (_isSpawnRequestPending)
        {
            return;
        }

        if (_useSpawnArrange)
        {
            SpawnFromArrange();
            return;
        }

        if (!IsSpawnRoleResolved())
        {
            return;
        }

        _isSpawnRequestPending = true;

        if (PhotonNetwork.IsMasterClient)
        {
            TryAssignSpawnPointAndBroadcast(PhotonNetwork.LocalPlayer.ActorNumber);
            return;
        }

        photonView.RPC(nameof(RPC_RequestSpawnPoint), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RPC_RequestSpawnPoint(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        TryAssignSpawnPointAndBroadcast(actorNumber);
    }

    [PunRPC]
    private void RPC_AssignAndSpawn(int actorNumber, bool useSurgeonSpawnPoint, int spawnIndex)
    {
        ApplySpawnSelection(actorNumber, new SpawnSelection(useSurgeonSpawnPoint, spawnIndex));

        if (PhotonNetwork.LocalPlayer == null || actorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            return;
        }

        SpawnAtIndex(useSurgeonSpawnPoint, spawnIndex);
    }

    private void TryAssignSpawnPointAndBroadcast(int actorNumber)
    {
        bool useSurgeonSpawnPoint = ShouldUseSurgeonSpawnPoint(actorNumber);
        int spawnIndex = GetAvailableSpawnPointIndex(useSurgeonSpawnPoint);
        if (spawnIndex < 0)
        {
            Debug.LogError($"[PlayerSpawnManager] 플레이어 {actorNumber}에게 스폰 포인트를 할당할 수 없습니다.");

            if (PhotonNetwork.LocalPlayer != null && actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                _isSpawnRequestPending = false;
            }

            return;
        }

        ApplySpawnSelection(actorNumber, new SpawnSelection(useSurgeonSpawnPoint, spawnIndex));
        photonView.RPC(nameof(RPC_AssignAndSpawn), RpcTarget.All, actorNumber, useSurgeonSpawnPoint, spawnIndex);
    }

    private int GetAvailableSpawnPointIndex(bool useSurgeonSpawnPoint)
    {
        Transform[] targetSpawnPoints = GetTargetSpawnPoints(useSurgeonSpawnPoint);
        if (targetSpawnPoints == null || targetSpawnPoints.Length == 0)
        {
            Debug.LogError(useSurgeonSpawnPoint
                ? "[PlayerSpawnManager] 집도의 스폰 포인트가 설정되지 않았습니다."
                : "[PlayerSpawnManager] 스폰 포인트가 설정되지 않았습니다.");
            return -1;
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < targetSpawnPoints.Length; i++)
        {
            if (IsSpawnPointAvailable(useSurgeonSpawnPoint, i))
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0)
        {
            return -1;
        }

        return availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
    }

    private void SpawnFromArrange()
    {
        if (_spawnArea == null)
        {
            Debug.LogWarning("[PlayerSpawnManager] 스폰 영역 콜라이더가 설정되지 않았습니다.");
            return;
        }

        SpawnAtPosition(GetRandomPointInSpawnArrange(_spawnArea));
    }

    private void SpawnAtIndex(bool useSurgeonSpawnPoint, int spawnIndex)
    {
        Transform[] targetSpawnPoints = GetTargetSpawnPoints(useSurgeonSpawnPoint);
        if (targetSpawnPoints == null ||
            spawnIndex < 0 ||
            spawnIndex >= targetSpawnPoints.Length ||
            targetSpawnPoints[spawnIndex] == null)
        {
            Debug.LogError($"[PlayerSpawnManager] 잘못된 스폰 인덱스입니다: {spawnIndex}.");
            _isSpawnRequestPending = false;
            return;
        }

        SpawnAtPosition(targetSpawnPoints[spawnIndex].position);
    }

    private void SpawnAtPosition(Vector3 spawnPosition)
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawnManager] 플레이어 프리팹이 할당되지 않았습니다.");
            _isSpawnRequestPending = false;
            return;
        }

        Quaternion backwardRotation = Quaternion.Euler(0f, 180f, 0f);
        _player = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPosition, backwardRotation);

        if (_player == null)
        {
            Debug.LogError("[PlayerSpawnManager] 플레이어 프리팹 생성에 실패했습니다.");
            _isSpawnRequestPending = false;
            return;
        }

        _hasSpawnedLocalPlayer = true;
        _isSpawnRequestPending = false;

        OnPlayerSpawned?.Invoke(_player);
        PlayerProperty.SetReadyState(false);
    }

    private Vector3 GetRandomPointInSpawnArrange(Collider spawnArrange)
    {
        Bounds bounds = spawnArrange.bounds;

        float randomX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, bounds.center.y, randomZ);
    }

    private bool IsSpawnRoleResolved()
    {
        if (StageSceneConfig.Instance?.SurgeonSpawnPoint == null)
        {
            return true;
        }

        StagePreloader preloader = StagePreloader.Instance;
        return preloader == null || preloader.IsRoleAssignmentComplete;
    }

    private bool ShouldUseSurgeonSpawnPoint(int actorNumber)
    {
        if (StageSceneConfig.Instance?.SurgeonSpawnPoint == null)
        {
            return false;
        }

        int surgeonActorNumber = ResolveSurgeonActorNumber();
        return surgeonActorNumber > 0 && surgeonActorNumber == actorNumber;
    }

    private int ResolveSurgeonActorNumber()
    {
        StagePreloader preloader = StagePreloader.Instance;
        if (preloader != null && preloader.IsRoleAssignmentComplete && preloader.SurgeonActorNumber > 0)
        {
            return preloader.SurgeonActorNumber;
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (RoleProperties.GetPlayerRole(player) == RoleType.Surgeon)
            {
                return player.ActorNumber;
            }
        }

        return -1;
    }

    private Transform[] GetTargetSpawnPoints(bool useSurgeonSpawnPoint)
    {
        if (StageSceneConfig.Instance == null)
        {
            Debug.LogError("[PlayerSpawnManager] StageSceneConfig 인스턴스가 없습니다.");
            return null;
        }

        RoleType role = useSurgeonSpawnPoint ? RoleType.Surgeon : RoleType.None;
        return StageSceneConfig.Instance.GetSpawnPointsByRole(role);
    }

    private bool IsSpawnPointAvailable(bool useSurgeonSpawnPoint, int index)
    {
        if (useSurgeonSpawnPoint)
        {
            return _surgeonSpawnActorNumber <= 0;
        }

        return !_usedSpawnPoints.ContainsValue(index);
    }

    private void ApplySpawnSelection(int actorNumber, SpawnSelection selection)
    {
        _usedSpawnPoints.Remove(actorNumber);

        if (_surgeonSpawnActorNumber == actorNumber)
        {
            _surgeonSpawnActorNumber = -1;
        }

        if (selection.UseSurgeonSpawnPoint)
        {
            _surgeonSpawnActorNumber = actorNumber;
            return;
        }

        _usedSpawnPoints[actorNumber] = selection.Index;
    }

    private void SubscribeToRoleAssignment()
    {
        StagePreloader preloader = StagePreloader.Instance;
        if (preloader == null)
        {
            return;
        }

        preloader.RoleAssignmentCompleted -= HandleRoleAssignmentCompleted;
        preloader.RoleAssignmentCompleted += HandleRoleAssignmentCompleted;
    }

    private void UnsubscribeFromRoleAssignment()
    {
        StagePreloader preloader = StagePreloader.Instance;
        if (preloader == null)
        {
            return;
        }

        preloader.RoleAssignmentCompleted -= HandleRoleAssignmentCompleted;
    }

    private void HandleRoleAssignmentCompleted(int surgeonActorNumber)
    {
        TrySpawnLocalPlayer();
    }

    private void CleanupExistingLocalPlayerObject()
    {
        GameObject existingPlayer = GetExistingLocalPlayerObject();
        if (existingPlayer == null)
        {
            return;
        }

        PhotonView playerView = existingPlayer.GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && playerView != null && playerView.IsMine)
        {
            PhotonNetwork.Destroy(existingPlayer);
        }
        else
        {
            Destroy(existingPlayer);
        }

        if (_player == existingPlayer)
        {
            _player = null;
        }

        _hasSpawnedLocalPlayer = false;
        _isSpawnRequestPending = false;
    }

    private GameObject GetExistingLocalPlayerObject()
    {
        if (_player != null)
        {
            return _player;
        }

        if (!PlayerRegistry.TryGetLocalPlayer(out PlayerController localPlayer) || localPlayer == null)
        {
            return null;
        }

        PhotonView playerView = localPlayer.PhotonView;
        if (playerView == null || !playerView.IsMine)
        {
            return null;
        }

        return localPlayer.gameObject;
    }
}
