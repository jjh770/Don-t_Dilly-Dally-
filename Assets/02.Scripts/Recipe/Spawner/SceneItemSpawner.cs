using System;
using System.Collections.Generic;
using DontDillyDally.Data;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 씬 시작 시 일반 공급원과 트레이 공급원을 배치하는 스포너입니다.
    // Photon 룸에서는 방장이 생성한 seed를 기준으로 모든 클라이언트가 같은 배치를 재현합니다.
    public class SceneItemSpawner : MonoBehaviourPunCallbacks
    {
        private const string SpawnSeedPropertyKey = "SceneItemSpawnerSeed";

        [Header("일반 아이템 카탈로그")]
        [Tooltip("씬에 배치할 일반 공급원 목록을 담고 있는 카탈로그")]
        public SceneItemSpawnCatalog SpawnCatalog;

        [Header("공통 프리팹")]
        [Tooltip("조합 도구 공급원을 생성할 때 사용하는 공통 프리팹")]
        public MixToolSource MixToolPrefab;

        [Tooltip("기본 재료 공급원을 생성할 때 사용하는 공통 프리팹")]
        public BasicMaterialSource BasicMaterialPrefab;

        [Tooltip("트레이 공급원을 생성할 때 사용하는 공통 프리팹")]
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

        private readonly List<GameObject> _spawnedObjects = new();

        private bool _hasSpawnedSceneObjects;

        private void Start()
        {
            TryInitializeSpawnLayout();
        }

        public override void OnJoinedRoom()
        {
            TryInitializeSpawnLayout();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            TryInitializeSpawnLayout();
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged == null || !propertiesThatChanged.ContainsKey(SpawnSeedPropertyKey))
                return;

            TryInitializeSpawnLayout();
        }

        [ContextMenu("중복 없이 일반 아이템 배치")]
        public void SpawnUniqueItems()
        {
            if (!ValidateItemSpawner())
                return;

            SpawnUniqueItems(CreateShuffledEntries(GenerateSeed()));
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
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                GameObject spawnedObject = _spawnedObjects[i];
                if (spawnedObject != null)
                    Destroy(spawnedObject);
            }

            _spawnedObjects.Clear();
            _hasSpawnedSceneObjects = false;
        }

        private void TryInitializeSpawnLayout()
        {
            if (_hasSpawnedSceneObjects)
                return;

            if (!SpawnItemsOnStart && !SpawnTraysOnStart)
                return;

            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                SpawnSceneObjects(GenerateSeed());
                return;
            }

            if (TryGetSpawnSeed(out int existingSeed))
            {
                SpawnSceneObjects(existingSeed);
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
                return;

            int newSeed = GenerateSeed();
            Hashtable properties = new Hashtable
            {
                { SpawnSeedPropertyKey, newSeed }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
            SpawnSceneObjects(newSeed);
        }

        private bool TryGetSpawnSeed(out int seed)
        {
            seed = default;

            if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
                return false;

            if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SpawnSeedPropertyKey, out object seedObject))
                return false;

            if (seedObject is int intSeed)
            {
                seed = intSeed;
                return true;
            }

            return false;
        }

        private void SpawnSceneObjects(int seed)
        {
            if (_hasSpawnedSceneObjects)
                return;

            if (ClearBeforeSpawn)
                ClearSpawnedItems();

            if (SpawnItemsOnStart)
                SpawnUniqueItems(CreateShuffledEntries(seed));

            if (SpawnTraysOnStart)
                SpawnTrays();

            _hasSpawnedSceneObjects = true;
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

        private void SpawnUniqueItems(List<SceneItemSpawnEntry> entries)
        {
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
                    $"[SceneItemSpawner] 일반 아이템 스폰 위치가 부족해 {entries.Count - SpawnPoints.Count}개를 배치하지 못했습니다.");
            }
        }

        private List<SceneItemSpawnEntry> CreateShuffledEntries(int seed)
        {
            List<SceneItemSpawnEntry> entries = SpawnCatalog.GetValidEntries();
            ShuffleEntries(entries, seed);
            return entries;
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
            MixToolSource prefab = entry.SourcePrefabOverride != null ? entry.SourcePrefabOverride : MixToolPrefab;
            Transform parent = SpawnedItemParent != null ? SpawnedItemParent : null;
            MixToolSource spawnedToolSource = Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation,
                parent);

            spawnedToolSource.name = $"{entry.GetDefaultName()}Source";
            spawnedToolSource.Initialize(entry.ToolType);
            _spawnedObjects.Add(spawnedToolSource.gameObject);
        }

        private void SpawnBasicMaterial(SceneItemSpawnEntry entry, Transform spawnPoint)
        {
            Transform parent = SpawnedItemParent != null ? SpawnedItemParent : null;
            BasicMaterialSource spawnedMaterialSource = Instantiate(
                BasicMaterialPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                parent);

            spawnedMaterialSource.name = $"{entry.GetDefaultName()}Source";
            spawnedMaterialSource.Initialize(entry.MaterialType);
            _spawnedObjects.Add(spawnedMaterialSource.gameObject);
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
            _spawnedObjects.Add(spawnedTraySource.gameObject);
        }

        private static int GenerateSeed()
        {
            return Environment.TickCount ^ Guid.NewGuid().GetHashCode();
        }

        private static void ShuffleEntries(List<SceneItemSpawnEntry> entries, int seed)
        {
            System.Random random = new(seed);

            for (int i = entries.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(0, i + 1);
                SceneItemSpawnEntry temp = entries[i];
                entries[i] = entries[randomIndex];
                entries[randomIndex] = temp;
            }
        }
    }
}
