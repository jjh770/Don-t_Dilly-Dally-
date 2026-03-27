using Photon.Pun;
using System;
using UnityEngine;

public class PlayerSpawnManager : PunSingleton<PlayerSpawnManager>
{
    [SerializeField] private Transform[] _spawnPoints;

    [SerializeField] private Collider _spawnArrange;

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
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("등록된 스폰 포인트가 없습니다.");
            return;
        }

        //int randomIndex = UnityEngine.Random.Range(0, _spawnPoints.Length);
        int Index = PhotonServerManager.Instance.CountOfPlayers;
        Vector3 spawnPosition = GetRandomPointInBoxCollider();

        // 리소스 폴더에서 "Player" 이름을 가진 프리팹을 생성하고, 서버에 등록함
        // 리소스 폴더는 좋지 않음 => 다른 방법을 찾아보자
        Quaternion backwardRotation = Quaternion.Euler(0, 180f, 0);
        _player = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPosition, backwardRotation);

        

        if (_player == null)
        {
            Debug.LogWarning("플레이어 프리팹을 생성하는 데 실패했습니다. 'Player' 프리팹이 Resources 폴더에 있는지 확인하세요.");
            return;
        }

        OnPlayerSpawned?.Invoke(_player);
        PlayerProperty.SetReadyState(false);
    }

    public Vector3 GetRandomPointInBoxCollider()
    {
        Vector3 center = _spawnArrange.bounds.center;
        Vector3 size = _spawnArrange.bounds.size;

        float randomX = UnityEngine.Random.Range(center.x - size.x / 2f, center.x + size.x / 2f);
        float randomZ = UnityEngine.Random.Range(center.z - size.z / 2f, center.z + size.z / 2f);

        return new Vector3(randomX, 0f, randomZ);
    }
}
