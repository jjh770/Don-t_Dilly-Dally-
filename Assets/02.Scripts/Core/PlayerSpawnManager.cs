using System;
using Photon.Pun;
using UnityEngine;

public class PlayerSpawnManager : PunSingleton<PlayerSpawnManager>
{
    public Transform _spawnPoint;

    [SerializeField] private GameObject _playerPrefab;

    private GameObject _player;

    public event Action OnRespawn;

    private bool _hasSpawnedLocalPlayer;

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
        if (_spawnPoint == null)
        {
            Debug.LogWarning("등록된 스폰 포인트가 없습니다.");
            return;
        }

        Vector3 spawnPosition = _spawnPoint.position;

        // 리소스 폴더에서 "Player" 이름을 가진 프리팹을 생성하고, 서버에 등록함
        // 리소스 폴더는 좋지 않음 => 다른 방법을 찾아보자
        _player = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPosition, Quaternion.identity);

        if (_player == null)
        {
            Debug.LogWarning("플레이어 프리팹을 생성하는 데 실패했습니다. 'Player' 프리팹이 Resources 폴더에 있는지 확인하세요.");
            return;
        }

        OnPlayerSpawned?.Invoke(_player);
    }
}
