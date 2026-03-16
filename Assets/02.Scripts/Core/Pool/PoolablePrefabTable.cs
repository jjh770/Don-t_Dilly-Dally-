using System;
using System.Collections.Generic;
using UnityEngine;

// 풀링 대상 프리팹 목록을 관리하는 ScriptableObject
// 생성: Project창 우클릭 → Create → DDD/Pool/PrefabTable
[CreateAssetMenu(fileName = "PoolablePrefabTable", menuName = "DDD/Pool/PrefabTable")]
public class PoolablePrefabTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public GameObject prefab;
        [Min(1)] public int initialSize = 5;
        public string PrefabId => prefab != null ? prefab.name : string.Empty;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, Entry> _table;

    // PunPoolManager.Awake에서 호출
    public void Initialize()
    {
        _table = new Dictionary<string, Entry>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning("[PrefabTable] 프리팹이 비어있는 항목이 있습니다. Inspector를 확인하세요.");
                continue;
            }
            if (!_table.TryAdd(entry.PrefabId, entry))
            {
                Debug.LogWarning($"[PrefabTable] '{entry.PrefabId}' 이름의 프리팹이 중복 등록되었습니다.");
            }
        }
    }

    public bool TryGetEntry(string prefabId, out Entry entry)
    {
        if (_table == null)
        {
            Debug.LogError("[PrefabTable] Initialize()가 호출되지 않았습니다.");
            entry = null;
            return false;
        }
        return _table.TryGetValue(prefabId, out entry);
    }

    public IEnumerable<Entry> GetAllEntries() => entries;
}
