using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerSpawnManager : PunSingleton<PlayerSpawnManager>
{
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private GameObject _playerPrefab;

    private GameObject _player;
    private bool _hasSpawnedLocalPlayer;

    private Dictionary<int, int> _usedSpawnPoints = new Dictionary<int, int>();

    public event Action OnRespawn;
    public event Action<GameObject> OnPlayerSpawned;

    public override void OnJoinedRoom()
    {
        TrySpawnLocalPlayer();
    }

    public void Start()
    {
        TrySpawnLocalPlayer();
    }

    private void TrySpawnLocalPlayer()
    {
        if (!PhotonNetwork.InRoom) return;
        if (_hasSpawnedLocalPlayer) return;

        _hasSpawnedLocalPlayer = true;

        if (PhotonNetwork.IsMasterClient)
        {
            int spawnIndex = GetAvailableSpawnPointIndex();
            if (spawnIndex < 0)
            {
                Debug.LogError("[PlayerSpawnManager] 사용 가능한 스폰 포인트가 없습니다.");
                return;
            }

            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            _usedSpawnPoints[actorNumber] = spawnIndex;

            photonView.RPC(nameof(RPC_SyncSpawnPoint), RpcTarget.Others, actorNumber, spawnIndex);
            SpawnAt(spawnIndex);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestSpawnPoint), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    private void RPC_RequestSpawnPoint(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int spawnIndex = GetAvailableSpawnPointIndex();
        if (spawnIndex < 0)
        {
            Debug.LogError($"[PlayerSpawnManager] 플레이어 {actorNumber}에게 할당할 스폰 포인트가 없습니다.");
            return;
        }

        _usedSpawnPoints[actorNumber] = spawnIndex;
        photonView.RPC(nameof(RPC_AssignSpawnPoint), RpcTarget.All, actorNumber, spawnIndex);
    }

    [PunRPC]
    private void RPC_SyncSpawnPoint(int actorNumber, int spawnIndex)
    {
        _usedSpawnPoints[actorNumber] = spawnIndex;
    }

    [PunRPC]
    private void RPC_AssignSpawnPoint(int actorNumber, int spawnIndex)
    {
        _usedSpawnPoints[actorNumber] = spawnIndex;

        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            SpawnAt(spawnIndex);
        }
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

    private void SpawnAt(int spawnIndex)
    {
        Vector3 spawnPosition = _spawnPoints[spawnIndex].position;
        Quaternion backwardRotation = Quaternion.Euler(0, 180f, 0);

        _player = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPosition, backwardRotation);

        if (_player == null)
        {
            Debug.LogError("[PlayerSpawnManager] 플레이어 프리팹 생성 실패. Resources 폴더에 프리팹이 있는지 확인하세요.");
            return;
        }

        OnPlayerSpawned?.Invoke(_player);
        PlayerProperty.SetReadyState(false);
    }

    public void Spawn()
    {
        TrySpawnLocalPlayer();
    }
}
