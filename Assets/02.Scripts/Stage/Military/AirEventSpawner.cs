using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class AirEventSpawner : MonoBehaviourPun
{
    [SerializeField] private GameObject[] _airEventPrefabs;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _minSpawnInterval = 30;
    [SerializeField] private float _maxSpawnInterval = 60;
    [SerializeField] private float _moveDuration = 10f;
    [SerializeField] private float _moveSpeed = 10f;

    [Header("카메라 쉐이크")]
    [SerializeField] private float _maxShakeIntensity = 0.15f;
    [SerializeField] private float _maxShakeDistance = 30f;

    [Header("헬기, 전투기 SFX")]
    [SerializeField]
    private SFXKey[] _helicopterSfxKeys =
    {
        SFXKey.AmbHelicopter1,
        SFXKey.AmbHelicopter2
    };

    [SerializeField]
    private SFXKey[] _jetSfxKeys =
    {
        SFXKey.AmbJetFly1,
        SFXKey.AmbJetFly2,
        SFXKey.AmbJetFly3
    };

    private float _nextSpawnTime;

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SetNextSpawnTime();
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (Time.time >= _nextSpawnTime)
        {
            RequestSpawnAirEvent();
            SetNextSpawnTime();
        }
    }

    private void SetNextSpawnTime()
    {
        _nextSpawnTime = Time.time + Random.Range(_minSpawnInterval, _maxSpawnInterval);
    }

    private void RequestSpawnAirEvent()
    {
        if (_spawnPoints.Length == 0 || _airEventPrefabs.Length == 0) return;

        int spawnPointIndex = Random.Range(0, _spawnPoints.Length);
        int prefabIndex = Random.Range(0, _airEventPrefabs.Length);
        SFXKey sfxKey;

        if (prefabIndex == _airEventPrefabs.Length - 1)
        {
            sfxKey = GetRandomSfx(_helicopterSfxKeys);
        }
        else
        {
            sfxKey = GetRandomSfx(_jetSfxKeys);
        }

        photonView.RPC(nameof(RPC_SpawnAirEvent), RpcTarget.All, spawnPointIndex, prefabIndex, (int)sfxKey);
    }

    private static SFXKey GetRandomSfx(SFXKey[] keys)
    {
        if (keys == null || keys.Length == 0)
        {
            return SFXKey.None;
        }

        return keys[Random.Range(0, keys.Length)];
    }

    [PunRPC]
    private void RPC_SpawnAirEvent(int spawnPointIndex, int prefabIndex, int sfxKey)
    {
        if (spawnPointIndex < 0 || spawnPointIndex >= _spawnPoints.Length) return;
        if (prefabIndex < 0 || prefabIndex >= _airEventPrefabs.Length) return;

        Transform spawnPoint = _spawnPoints[spawnPointIndex];
        GameObject prefab = _airEventPrefabs[prefabIndex];
        GameObject spawnedObject = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        Vector3 targetPosition = new Vector3(0f, spawnPoint.position.y, 0f);
        Vector3 direction = (targetPosition - spawnPoint.position).normalized;

        if (direction != Vector3.zero)
        {
            spawnedObject.transform.rotation = Quaternion.LookRotation(direction);
        }

        if (sfxKey != (int)SFXKey.None)
        {
            SoundManager.Instance.Play((SFXKey)sfxKey, SoundType.Local);
        }

        AirEventMover mover = spawnedObject.AddComponent<AirEventMover>();
        mover.Initialize(direction, _moveDuration, _moveSpeed, _maxShakeIntensity, _maxShakeDistance);
    }
}
