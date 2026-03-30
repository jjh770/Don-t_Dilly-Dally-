using Photon.Pun;
using System;
using UnityEngine;

public class PlayerSpawnManager : PunSingleton<PlayerSpawnManager>
{
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private GameObject _playerPrefab;

    private GameObject _player;
    private bool _hasSpawnedLocalPlayer;

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

        Spawn();
        _hasSpawnedLocalPlayer = _player != null;
    }

    public void Spawn()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[PlayerSpawnManager] 스폰 포인트가 설정되지 않았습니다.");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _spawnPoints.Length);
        Vector3 spawnPosition = _spawnPoints[randomIndex].position;
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
}
