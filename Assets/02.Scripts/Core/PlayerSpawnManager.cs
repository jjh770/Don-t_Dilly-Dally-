using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerSpawnManager : PunSingleton<PlayerSpawnManager>
{
    [Header("Spawn Mode")]
    [SerializeField] private bool _useSpawnArrange;

    [Header("Point Spawn")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Area Spawn")]
    [SerializeField] private Collider _spawnArrange;

    [Header("Player")]
    [SerializeField] private GameObject _playerPrefab;

    private readonly Dictionary<int, int> _usedSpawnPoints = new Dictionary<int, int>();

    private GameObject _player;
    private bool _hasSpawnedLocalPlayer;
    private bool _isSpawnRequestPending;

    public event Action<GameObject> OnPlayerSpawned;

    public override void OnJoinedRoom()
    {
        TrySpawnLocalPlayer();
    }

    public override void OnLeftRoom()
    {
        _usedSpawnPoints.Clear();
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
    }

    private void Start()
    {
        TrySpawnLocalPlayer();
    }

    private void TrySpawnLocalPlayer()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        if (_hasSpawnedLocalPlayer || _isSpawnRequestPending)
        {
            return;
        }

        if (_useSpawnArrange)
        {
            SpawnFromArrange();
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
    private void RPC_AssignAndSpawn(int actorNumber, int spawnIndex)
    {
        _usedSpawnPoints[actorNumber] = spawnIndex;

        if (PhotonNetwork.LocalPlayer == null || actorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            return;
        }

        SpawnAtIndex(spawnIndex);
    }

    public void Spawn()
    {
        TrySpawnLocalPlayer();
    }

    private void TryAssignSpawnPointAndBroadcast(int actorNumber)
    {
        int spawnIndex = GetAvailableSpawnPointIndex();
        if (spawnIndex < 0)
        {
            Debug.LogError($"[PlayerSpawnManager] 플레이어 {actorNumber}에게 스폰 포인트를 할당할 수 없습니다.");

            if (PhotonNetwork.LocalPlayer != null && actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                _isSpawnRequestPending = false;
            }

            return;
        }

        _usedSpawnPoints[actorNumber] = spawnIndex;
        photonView.RPC(nameof(RPC_AssignAndSpawn), RpcTarget.All, actorNumber, spawnIndex);
    }

    private int GetAvailableSpawnPointIndex()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[PlayerSpawnManager] 스폰 포인트가 설정되지 않았습니다.");
            return -1;
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            if (!_usedSpawnPoints.ContainsValue(i))
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
        if (_spawnArrange == null)
        {
            Debug.LogWarning("[PlayerSpawnManager] 스폰 영역 콜라이더가 할당되지 않았습니다.");
            return;
        }

        SpawnAtPosition(GetRandomPointInSpawnArrange());
    }

    private void SpawnAtIndex(int spawnIndex)
    {
        if (_spawnPoints == null || spawnIndex < 0 || spawnIndex >= _spawnPoints.Length || _spawnPoints[spawnIndex] == null)
        {
            Debug.LogError($"[PlayerSpawnManager] 잘못된 스폰 인덱스입니다: {spawnIndex}.");
            _isSpawnRequestPending = false;
            return;
        }

        SpawnAtPosition(_spawnPoints[spawnIndex].position);
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

    private Vector3 GetRandomPointInSpawnArrange()
    {
        Bounds bounds = _spawnArrange.bounds;

        float randomX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, bounds.center.y, randomZ);
    }
}
