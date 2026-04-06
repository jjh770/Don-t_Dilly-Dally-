using UnityEngine;

public class AirEventSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] airEventPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minSpawnInterval = 120f; // 2분
    [SerializeField] private float maxSpawnInterval = 240f; // 4분
    [SerializeField] private float moveDuration = 10f;
    [SerializeField] private float moveSpeed = 10f;

    private float nextSpawnTime;

    private void Start()
    {
        SetNextSpawnTime();
    }

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnAirEvent();
            SetNextSpawnTime();
        }
    }

    private void SetNextSpawnTime()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void SpawnAirEvent()
    {
        if (spawnPoints.Length == 0 || airEventPrefabs.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject prefab = airEventPrefabs[Random.Range(0, airEventPrefabs.Length)];
        GameObject spawnedObject = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        Vector3 targetPosition = new Vector3(0f, spawnPoint.position.y, 0f);
        Vector3 direction = (targetPosition - spawnPoint.position).normalized;

        if (direction != Vector3.zero)
        {
            spawnedObject.transform.rotation = Quaternion.LookRotation(direction);
        }

        AirEventMover mover = spawnedObject.AddComponent<AirEventMover>();
        mover.Initialize(direction, moveDuration, moveSpeed);
    }
}

public class AirEventMover : MonoBehaviour
{
    private Vector3 moveDirection;
    private float duration;
    private float speed;
    private float elapsedTime;

    public void Initialize(Vector3 direction, float moveDuration, float moveSpeed)
    {
        moveDirection = direction;
        duration = moveDuration;
        speed = moveSpeed;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= duration)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += moveDirection * speed * Time.deltaTime;
    }
}
