using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 씬 시작 시 일반 공급원과 트레이를 각각 배치하는 스포너입니다.
    public class SceneItemSpawner : MonoBehaviour
    {
        [Header("일반 아이템 카탈로그")]
        [Tooltip("씬에 배치할 일반 공급원 목록을 들고 있는 카탈로그")]
        public SceneItemSpawnCatalog SpawnCatalog;

        [Header("공통 프리팹")]
        [Tooltip("조합 도구 공급원을 생성할 때 사용할 공통 프리팹")]
        public MixToolSource MixToolPrefab;

        [Tooltip("기본 재료 공급원을 생성할 때 사용할 공통 프리팹")]
        public BasicMaterialSource BasicMaterialPrefab;

        [Tooltip("트레이 공급원을 생성할 때 사용할 공통 프리팹")]
        public TraySource TraySourcePrefab;

        [Header("일반 아이템 배치 위치")]
        [Tooltip("일반 공급원을 생성할 위치 목록")]
        public List<Transform> SpawnPoints = new List<Transform>();

        [Header("트레이 배치 위치")]
        [Tooltip("트레이를 생성할 위치 목록")]
        public List<Transform> TraySpawnPoints = new List<Transform>();

        [Tooltip("생성된 오브젝트를 정리해서 둘 부모 Transform")]
        public Transform SpawnedItemParent;

        [Header("실행 설정")]
        [Tooltip("씬 시작 시 자동으로 일반 공급원을 배치할지 여부")]
        public bool SpawnItemsOnStart = true;

        [Tooltip("씬 시작 시 자동으로 트레이를 배치할지 여부")]
        public bool SpawnTraysOnStart = true;

        [Tooltip("다시 배치하기 전에 기존 생성 오브젝트를 먼저 지울지 여부")]
        public bool ClearBeforeSpawn = true;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        private void Start()
        {
            if (ClearBeforeSpawn)
                ClearSpawnedItems();

            if (SpawnItemsOnStart)
                SpawnUniqueItems();

            if (SpawnTraysOnStart)
                SpawnTrays();
        }

        [ContextMenu("중복 없이 일반 아이템 배치")]
        public void SpawnUniqueItems()
        {
            if (!ValidateItemSpawner())
                return;

            List<SceneItemSpawnEntry> entries = SpawnCatalog.GetValidEntries();
            ShuffleEntries(entries);

            int spawnCount = Mathf.Min(entries.Count, SpawnPoints.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                SceneItemSpawnEntry entry = entries[i];
                Transform spawnPoint = SpawnPoints[i];

                if (spawnPoint == null)
                    continue;

                SpawnEntry(entry, spawnPoint);
            }

            if (SpawnPoints.Count < entries.Count)
            {
                Debug.LogWarning(
                    $"[SceneItemSpawner] 일반 아이템 스폰 위치가 부족해서 {entries.Count - SpawnPoints.Count}개를 배치하지 못했습니다.");
            }
        }

        [ContextMenu("트레이 배치")]
        public void SpawnTrays()
        {
            if (TraySourcePrefab == null)
            {
                Debug.LogWarning("[SceneItemSpawner] TraySourcePrefab이 연결되지 않았습니다.");
                return;
            }

            if (TraySpawnPoints == null || TraySpawnPoints.Count == 0)
            {
                Debug.LogWarning("[SceneItemSpawner] TraySpawnPoints가 비어 있습니다.");
                return;
            }

            for (int i = 0; i < TraySpawnPoints.Count; i++)
            {
                Transform spawnPoint = TraySpawnPoints[i];
                if (spawnPoint == null)
                    continue;

                SpawnTray(spawnPoint, i + 1);
            }
        }

        [ContextMenu("생성 오브젝트 비우기")]
        public void ClearSpawnedItems()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                GameObject spawnedObject = spawnedObjects[i];
                if (spawnedObject != null)
                    Destroy(spawnedObject);
            }

            spawnedObjects.Clear();
        }

        private bool ValidateItemSpawner()
        {
            if (SpawnCatalog == null)
            {
                Debug.LogWarning("[SceneItemSpawner] SpawnCatalog가 연결되지 않았습니다.");
                return false;
            }

            if (!SpawnCatalog.Validate())
                return false;

            if (SpawnPoints == null || SpawnPoints.Count == 0)
            {
                Debug.LogWarning("[SceneItemSpawner] SpawnPoints가 비어 있습니다.");
                return false;
            }

            if (SpawnCatalog.HasKind(SpawnItemKind.MixTool) && MixToolPrefab == null)
            {
                Debug.LogWarning("[SceneItemSpawner] MixToolPrefab이 연결되지 않았습니다.");
                return false;
            }

            if (SpawnCatalog.HasKind(SpawnItemKind.BasicMaterial) && BasicMaterialPrefab == null)
            {
                Debug.LogWarning("[SceneItemSpawner] BasicMaterialPrefab이 연결되지 않았습니다.");
                return false;
            }

            return true;
        }

        private void SpawnEntry(SceneItemSpawnEntry entry, Transform spawnPoint)
        {
            switch (entry.Kind)
            {
                case SpawnItemKind.MixTool:
                    SpawnMixTool(entry, spawnPoint);
                    break;

                case SpawnItemKind.BasicMaterial:
                    SpawnBasicMaterial(entry, spawnPoint);
                    break;
            }
        }

        private void SpawnMixTool(SceneItemSpawnEntry entry, Transform spawnPoint)
        {
            Transform parent = SpawnedItemParent != null ? SpawnedItemParent : null;
            MixToolSource spawnedToolSource = Instantiate(
                MixToolPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                parent);

            spawnedToolSource.name = entry.GetDefaultName();
            spawnedToolSource.Initialize(entry.ToolType);
            spawnedObjects.Add(spawnedToolSource.gameObject);
        }

        private void SpawnBasicMaterial(SceneItemSpawnEntry entry, Transform spawnPoint)
        {
            Transform parent = SpawnedItemParent != null ? SpawnedItemParent : null;
            BasicMaterialSource spawnedMaterialSource = Instantiate(
                BasicMaterialPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                parent);

            spawnedMaterialSource.name = entry.GetDefaultName();
            spawnedMaterialSource.Initialize(entry.MaterialType);
            spawnedObjects.Add(spawnedMaterialSource.gameObject);
        }

        private void SpawnTray(Transform spawnPoint, int trayIndex)
        {
            Transform parent = SpawnedItemParent != null ? SpawnedItemParent : null;
            TraySource spawnedTraySource = Instantiate(
                TraySourcePrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                parent);

            spawnedTraySource.name = $"TraySource_{trayIndex}";
            spawnedTraySource.ForceRespawn();
            spawnedObjects.Add(spawnedTraySource.gameObject);
        }

        private static void ShuffleEntries(List<SceneItemSpawnEntry> entries)
        {
            for (int i = entries.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                SceneItemSpawnEntry temp = entries[i];
                entries[i] = entries[randomIndex];
                entries[randomIndex] = temp;
            }
        }
    }
}
