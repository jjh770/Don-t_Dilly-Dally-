using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

// 사용 예시
// 생성: PhotonNetwork.Instantiate("BandagePrefab", position, rotation);
// 반납: PhotonNetwork.Destroy(gameObject);
public class PunPoolManager : PunSingleton<PunPoolManager>, IPunPrefabPool
{
    [SerializeField] private PoolablePrefabTable _prefabTable;

    private readonly Dictionary<string, Queue<GameObject>> _pools = new();
    private Transform _poolRoot;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        _poolRoot = new GameObject("[PUNPoolRoot]").transform;
        _poolRoot.SetParent(transform);

        _prefabTable.Initialize();
        PhotonNetwork.PrefabPool = this;
        WarmUp();
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(PhotonNetwork.PrefabPool, this))
        {
            // 씬 전용 풀 매니저가 제거될 때는 PUN 기본 풀로 되돌린다.
            PhotonNetwork.PrefabPool = null;
        }
    }

    // IPunPrefabPool 구현:
    // PUN2가 활성화 처리를 하므로 비활성 상태의 오브젝트를 반환해야 한다.
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        if (_prefabTable.TryGetEntry(prefabId, out _))
        {
            GameObject pooled = GetFromQueue(prefabId) ?? CreateNew(prefabId);
            if (pooled == null)
                return null;

            pooled.transform.SetParent(null);
            pooled.transform.SetPositionAndRotation(position, rotation);
            return pooled;
        }

        GameObject prefab = Resources.Load<GameObject>(prefabId);
        if (prefab == null)
        {
            Debug.LogError($"[PunPoolManager] '{prefabId}'를 찾을 수 없습니다.");
            return null;
        }

        return Object.Instantiate(prefab, position, rotation);
    }

    // IPunPrefabPool 구현:
    // PUN2가 SetActive(false)까지 처리한 뒤 호출하므로 큐에만 되돌려 놓는다.
    public void Destroy(GameObject go)
    {
        if (!go.TryGetComponent<PoolableObject>(out var poolable))
        {
            Object.Destroy(go);
            return;
        }

        // 풀 루트 아래로 되돌려 다음 재사용을 준비한다.
        go.transform.SetParent(_poolRoot);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        ReturnToQueue(poolable.PrefabId, go);
    }

    private void WarmUp()
    {
        foreach (var entry in _prefabTable.GetAllEntries())
        {
            if (entry.Prefab == null) continue;
            if (_pools.ContainsKey(entry.PrefabId)) continue;

            for (int i = 0; i < entry.InitialSize; i++)
            {
                var obj = CreateNew(entry.PrefabId);
                if (obj != null) ReturnToQueue(entry.PrefabId, obj);
            }
        }
    }

    private GameObject CreateNew(string prefabId)
    {
        if (!_prefabTable.TryGetEntry(prefabId, out var entry))
            return null;

        var obj = Object.Instantiate(entry.Prefab, _poolRoot);
        obj.SetActive(false);

        if (!obj.TryGetComponent<PoolableObject>(out var poolable))
            poolable = obj.AddComponent<PoolableObject>();

        poolable.PrefabId = prefabId;
        return obj;
    }

    private GameObject GetFromQueue(string prefabId)
    {
        if (_pools.TryGetValue(prefabId, out var queue) && queue.Count > 0)
            return queue.Dequeue();
        return null;
    }

    private void ReturnToQueue(string prefabId, GameObject obj)
    {
        if (!_pools.ContainsKey(prefabId))
            _pools[prefabId] = new Queue<GameObject>();

        _pools[prefabId].Enqueue(obj);
    }
}
