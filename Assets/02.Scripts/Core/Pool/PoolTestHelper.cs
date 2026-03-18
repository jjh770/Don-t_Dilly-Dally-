using UnityEngine;

// 풀링 디버그용 테스트 스크립트 (빌드에 포함되지 않음)
// 사용법: 아무 GameObject에 부착 → Play → 키 입력으로 테스트
// 테스트 완료 후 삭제해도 됩니다.
#if UNITY_EDITOR
public class PoolTestHelper : MonoBehaviour
{
    [Header("PrefabTable에 등록된 프리팹 이름 입력")]
    [SerializeField] private string _prefabId = "";

    [Header("생성 위치 (이 오브젝트 기준 오프셋)")]
    [SerializeField] private Vector3 _spawnOffset = new(0f, 1f, 2f);

    private readonly System.Collections.Generic.List<GameObject> _spawnedObjects = new();

    private void Update()
    {
        // Alpha1: 풀에서 꺼내기
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SpawnOne();
        }

        // Alpha2: 가장 최근 오브젝트 풀에 반납
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ReturnLast();
        }

        // Alpha3: 5개 연속 생성 (풀 소진 + 자동 확장 테스트)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            for (int i = 0; i < 5; i++)
            {
                SpawnOne();
            }
        }

        // Alpha4: 전부 반납
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ReturnAll();
        }
    }

    private void SpawnOne()
    {
        if (string.IsNullOrEmpty(_prefabId))
        {
            Debug.LogWarning("[PoolTest] PrefabId가 비어있습니다. Inspector에서 입력하세요.");
            return;
        }

        Vector3 pos = transform.position + _spawnOffset + Random.insideUnitSphere * 0.5f;
        GameObject obj = PunPoolManager.Instance.Instantiate(_prefabId, pos, Quaternion.identity);

        if (obj == null) return;

        obj.SetActive(true);
        _spawnedObjects.Add(obj);
        Debug.Log($"[PoolTest] 생성 완료 — 현재 활성: {_spawnedObjects.Count}개");
    }

    private void ReturnLast()
    {
        if (_spawnedObjects.Count == 0)
        {
            Debug.LogWarning("[PoolTest] 반납할 오브젝트가 없습니다.");
            return;
        }

        int lastIndex = _spawnedObjects.Count - 1;
        GameObject obj = _spawnedObjects[lastIndex];
        _spawnedObjects.RemoveAt(lastIndex);

        obj.SetActive(false);
        PunPoolManager.Instance.Destroy(obj);
        Debug.Log($"[PoolTest] 반납 완료 — 현재 활성: {_spawnedObjects.Count}개");
    }

    private void ReturnAll()
    {
        int count = _spawnedObjects.Count;
        for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
        {
            _spawnedObjects[i].SetActive(false);
            PunPoolManager.Instance.Destroy(_spawnedObjects[i]);
        }
        _spawnedObjects.Clear();
        Debug.Log($"[PoolTest] 전체 반납 완료 — {count}개 반납됨");
    }
}
#endif
