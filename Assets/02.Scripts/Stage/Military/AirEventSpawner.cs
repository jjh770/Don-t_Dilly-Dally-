using UnityEngine;

public class AirEventSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] airEventPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minSpawnInterval = 120f; // 2분
    [SerializeField] private float maxSpawnInterval = 240f; // 4분
    [SerializeField] private float moveDuration = 10f;
    [SerializeField] private float moveSpeed = 10f;

    [Header("카메라 쉐이크")]
    [SerializeField] private float maxShakeIntensity = 0.15f;
    [SerializeField] private float maxShakeDistance = 30f;

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
        mover.Initialize(direction, moveDuration, moveSpeed, maxShakeIntensity, maxShakeDistance);
    }
}

public class AirEventMover : MonoBehaviour
{
    private Vector3 moveDirection;
    private float duration;
    private float speed;
    private float elapsedTime;

    private float maxShakeIntensity;
    private float maxShakeDistance;

    public void Initialize(Vector3 direction, float moveDuration, float moveSpeed, float shakeIntensity, float shakeDistance)
    {
        moveDirection = direction;
        duration = moveDuration;
        speed = moveSpeed;
        maxShakeIntensity = shakeIntensity;
        maxShakeDistance = shakeDistance;
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

        ApplyCameraShake();
    }

    private void ApplyCameraShake()
    {
        if (CameraShakeManager.Instance == null) return;

        // 중심점(0, y, 0)과의 거리 계산 (y축 무시)
        Vector3 pos = transform.position;
        float distanceToCenter = new Vector2(pos.x, pos.z).magnitude;

        // 거리에 따른 쉐이크 강도 (가까울수록 강함)
        float intensity = Mathf.Lerp(maxShakeIntensity, 0f, distanceToCenter / maxShakeDistance);

        CameraShakeManager.Instance.SetIntensity(intensity);
    }
}
