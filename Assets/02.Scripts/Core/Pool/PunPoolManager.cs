using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

// [사용법]
// 생성: PhotonNetwork.Instantiate("BandagePrefab", position, rotation);
// 반납: PhotonNetwork.Destroy(gameObject);
public class PunPoolManager : PunPersistentSingleton<PunPoolManager>, IPunPrefabPool
{
    [SerializeField] private PoolablePrefabTable prefabTable;

    private readonly Dictionary<string, Queue<GameObject>> _pools = new();
    private Transform _poolRoot;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        _poolRoot = new GameObject("[PUNPoolRoot]").transform;
        _poolRoot.SetParent(transform);

        prefabTable.Initialize();
        PhotonNetwork.PrefabPool = this;
        WarmUp();
    }

    // IPunPrefabPool — 반드시 비활성화 상태로 반환 (PUN2가 활성화 처리)
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        GameObject obj = GetFromQueue(prefabId);

        if (obj == null)
        {
            obj = CreateNew(prefabId);
        }

        if (obj == null)
        {
            Debug.LogError($"[PunPoolManager] '{prefabId}'를 찾을 수 없습니다. PrefabTable을 확인하세요.");
            return null;
        }

        obj.transform.SetParent(null);
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    // IPunPrefabPool — PUN2가 SetActive(false) 처리 후 호출
    public void Destroy(GameObject go)
    {
        var poolable = go.GetComponent<PoolableObject>();
        if (poolable == null)
        {
            Object.Destroy(go);
            return;
        }
        
        // 오브젝트 초기화 후 풀로 반환
        go.transform.SetParent(_poolRoot);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        ReturnToQueue(poolable.PrefabId, go);
    }

    private void WarmUp()
    {
        foreach (var entry in prefabTable.GetAllEntries())
        {
            if (entry.prefab == null) continue;
            if (_pools.ContainsKey(entry.PrefabId)) continue;

            for (int i = 0; i < entry.initialSize; i++)
            {
                var obj = CreateNew(entry.PrefabId);
                if (obj != null) ReturnToQueue(entry.PrefabId, obj);
            }
        }
    }

    private GameObject CreateNew(string prefabId)
    {
        if (!prefabTable.TryGetEntry(prefabId, out var entry))
            return null;

        var obj = Object.Instantiate(entry.prefab, _poolRoot);
        obj.SetActive(false);

        var poolable = obj.GetComponent<PoolableObject>();
        if (poolable == null)
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
