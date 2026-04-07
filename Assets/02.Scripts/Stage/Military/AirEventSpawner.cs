using UnityEngine;

public class AirEventSpawner : MonoBehaviour
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

    private float _nextSpawnTime;

    private void Start()
    {
        SetNextSpawnTime();
    }

    private void Update()
    {
        if (Time.time >= _nextSpawnTime)
        {
            SpawnAirEvent();
            SetNextSpawnTime();
        }
    }

    private void SetNextSpawnTime()
    {
        _nextSpawnTime = Time.time + Random.Range(_minSpawnInterval, _maxSpawnInterval);
    }

    private void SpawnAirEvent()
    {
        if (_spawnPoints.Length == 0 || _airEventPrefabs.Length == 0) return;

        Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        GameObject prefab = _airEventPrefabs[Random.Range(0, _airEventPrefabs.Length)];
        GameObject spawnedObject = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        Vector3 targetPosition = new Vector3(0f, spawnPoint.position.y, 0f);
        Vector3 direction = (targetPosition - spawnPoint.position).normalized;

        if (direction != Vector3.zero)
        {
            spawnedObject.transform.rotation = Quaternion.LookRotation(direction);
        }

        AirEventMover mover = spawnedObject.AddComponent<AirEventMover>();
        mover.Initialize(direction, _moveDuration, _moveSpeed, _maxShakeIntensity, _maxShakeDistance);
    }
}
