using DontDillyDally.StageFlow;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 씬 시작 시 일반 공급원과 트레이 공급원을 배치하는 스포너입니다.
    // Photon 룸에서는 방장이 공급원을 RoomObject로 생성하고 모든 클라이언트가 같은 오브젝트를 공유합니다.
    public class SceneItemSpawner : MonoBehaviourPunCallbacks
    {
        [Header("공통 프리팹")]
        [Tooltip("조합 도구 공급원을 생성할 때 사용하는 공통 프리팹")]
        [SerializeField] private MixToolSource _mixToolPrefab;

        [Tooltip("기본 재료 공급원을 생성할 때 사용하는 공통 프리팹")]
        [SerializeField] private BasicMaterialSource _basicMaterialPrefab;

        [Tooltip("트레이 공급원을 생성할 때 사용하는 공통 프리팹")]
        [SerializeField] private TraySource _traySourcePrefab;

        [Tooltip("생성된 오브젝트를 정리해서 둘 부모 Transform")]
        [SerializeField] private Transform _spawnedItemParent;

        [Header("실행 설정")]
        [Tooltip("씬 시작 시 자동으로 일반 공급원을 배치할지 여부")]
        [SerializeField] private bool _spawnItemsOnStart = true;

        [Tooltip("씬 시작 시 자동으로 트레이를 배치할지 여부")]
        [SerializeField] private bool _spawnTraysOnStart = true;

        [Tooltip("다시 배치하기 전에 기존 생성 오브젝트를 먼저 지울지 여부")]
        [SerializeField] private bool _clearBeforeSpawn = true;

        [Header("스테이지별 아이템 설정")]
        [Tooltip("씬에 배치된 스테이지별 아이템 설정 목록")]
        [SerializeField] private List<StageItemConfig> _stageItemConfigs = new List<StageItemConfig>();

        private SceneItemSpawnCatalog _spawnCatalog;
        private List<Transform> _spawnPoints = new List<Transform>();
        private List<Transform> _traySpawnPoints = new List<Transform>();

        private readonly List<GameObject> _spawnedObjects = new();

        private bool _hasSpawnedSceneObjects;
        private bool _isConfigured;

        private bool _isSubscribed;

        private void Start()
        {
            TrySubscribe();
        }

        public override void OnEnable()
        {
            TrySubscribe();
        }

        public override void OnDisable()
        {
            if (_isSubscribed && StageFlowManager.Instance != null)
            {
                StageFlowManager.Instance.OnStageDataChanged -= HandleStageDataChanged;
                _isSubscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (_isSubscribed || StageFlowManager.Instance == null)
                return;

            StageFlowManager.Instance.OnStageDataChanged += HandleStageDataChanged;
            _isSubscribed = true;
        }

        private void HandleStageDataChanged(StageRuntimeData stageData)
        {
            if (stageData == null)
                return;

            StageItemConfig config = _stageItemConfigs.FirstOrDefault(c => c != null && c.StageId == stageData.StageId);
            if (config == null)
            {
                Debug.LogWarning($"[SceneItemSpawner] StageId '{stageData.StageId}'에 해당하는 StageItemConfig를 찾지 못했습니다.");
                _spawnCatalog = null;
                _spawnPoints = new List<Transform>();
                _traySpawnPoints = new List<Transform>();
                _isConfigured = false;
                return;
            }

            TryApplyConfig(config);
        }

        private void TryApplyConfig(StageItemConfig config)
        {
            ClearSpawnedItems();

            if (!ValidateConfig(config))
            {
                _spawnCatalog = null;
                _spawnPoints = new List<Transform>();
                _traySpawnPoints = new List<Transform>();
                _isConfigured = false;
                return;
            }

            _spawnCatalog = config.SpawnCatalog;
            _spawnPoints = config.SpawnPoints;
            _traySpawnPoints = config.TraySpawnPoints;
            _isConfigured = true;

            TryInitializeSpawnLayout();
        }

        private bool ValidateConfig(StageItemConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[SceneItemSpawner] StageItemConfig가 비어 있습니다.");
                return false;
            }

            if (_spawnItemsOnStart)
            {
                if (config.SpawnCatalog == null)
                {
                    Debug.LogWarning($"[SceneItemSpawner] 스테이지 '{config.StageId}'의 SpawnCatalog가 비어 있습니다.");
                    return false;
                }

                if (!config.SpawnCatalog.Validate())
                    return false;

                if (config.SpawnPoints == null || config.SpawnPoints.Count == 0)
                {
                    Debug.LogWarning($"[SceneItemSpawner] 스테이지 '{config.StageId}'의 SpawnPoints가 비어 있습니다.");
                    return false;
                }

                if (config.SpawnCatalog.HasKind(SpawnItemKind.MixTool) && _mixToolPrefab == null)
                {
                    Debug.LogWarning("[SceneItemSpawner] MixToolSource 프리팹이 연결되지 않았습니다.");
                    return false;
                }

                if (config.SpawnCatalog.HasKind(SpawnItemKind.BasicMaterial) && _basicMaterialPrefab == null)
                {
                    Debug.LogWarning("[SceneItemSpawner] BasicMaterialSource 프리팹이 연결되지 않았습니다.");
                    return false;
                }
            }

            if (_spawnTraysOnStart)
            {
                if (_traySourcePrefab == null)
                {
                    Debug.LogWarning("[SceneItemSpawner] TraySource 프리팹이 연결되지 않았습니다.");
                    return false;
                }

                if (config.TraySpawnPoints == null || config.TraySpawnPoints.Count == 0)
                {
                    Debug.LogWarning($"[SceneItemSpawner] 스테이지 '{config.StageId}'의 TraySpawnPoints가 비어 있습니다.");
                    return false;
                }
            }

            return true;
        }

        public override void OnJoinedRoom()
        {
            if (_isConfigured)
                TryInitializeSpawnLayout();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (_isConfigured)
                TryInitializeSpawnLayout();
        }

        [ContextMenu("트레이 배치")]
        public void SpawnTrays()
        {
            if (_traySourcePrefab == null)
            {
                Debug.LogWarning("[SceneItemSpawner] TraySourcePrefab이 연결되지 않았습니다.");
                return;
            }

            if (_traySpawnPoints == null || _traySpawnPoints.Count == 0)
            {
                Debug.LogWarning("[SceneItemSpawner] TraySpawnPoints가 비어 있습니다.");
                return;
            }

            for (int i = 0; i < _traySpawnPoints.Count; i++)
            {
                Transform spawnPoint = _traySpawnPoints[i];
                if (spawnPoint == null)
                    continue;

                SpawnTraySource(spawnPoint, i + 1);
            }
        }

        [ContextMenu("생성 오브젝트 비우기")]
        public void ClearSpawnedItems()
        {
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                GameObject spawnedObject = _spawnedObjects[i];
                if (spawnedObject != null)
                {
                    if (PhotonNetwork.InRoom)
                    {
                        if (PhotonNetwork.IsMasterClient)
                        {
                            PhotonNetwork.Destroy(spawnedObject);
                        }
                    }
                    else
                    {
                        Destroy(spawnedObject);
                    }
                }
            }

            _spawnedObjects.Clear();
            _hasSpawnedSceneObjects = false;
        }

        private void TryInitializeSpawnLayout()
        {
            if (_hasSpawnedSceneObjects)
                return;

            if (!_spawnItemsOnStart && !_spawnTraysOnStart)
                return;

            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                SpawnSceneObjects();
                return;
            }

            if (HasExistingSpawnSources())
            {
                _hasSpawnedSceneObjects = true;
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
                return;

            SpawnSceneObjects();
        }

        private void SpawnSceneObjects()
        {
            if (_hasSpawnedSceneObjects)
                return;

            if (_clearBeforeSpawn)
                ClearSpawnedItems();

            if (_spawnItemsOnStart)
                SpawnUniqueItems(CreateShuffledEntries());

            if (_spawnTraysOnStart)
                SpawnTrays();

            _hasSpawnedSceneObjects = true;
        }

        private void SpawnUniqueItems(List<SceneItemSpawnEntry> entries)
        {
            int spawnCount = Mathf.Min(entries.Count, _spawnPoints.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                SceneItemSpawnEntry entry = entries[i];
                Transform spawnPoint = _spawnPoints[i];

                if (spawnPoint == null)
                    continue;

                SpawnEntrySource(entry, spawnPoint);
            }

            if (_spawnPoints.Count < entries.Count)
            {
                Debug.LogWarning(
                    $"[SceneItemSpawner] 일반 아이템 스폰 위치가 부족해 {entries.Count - _spawnPoints.Count}개를 배치하지 못했습니다.");
            }
        }

        private List<SceneItemSpawnEntry> CreateShuffledEntries()
        {
            List<SceneItemSpawnEntry> entries = _spawnCatalog.GetValidEntries();
            ShuffleEntries(entries);
            return entries;
        }

        private void SpawnEntrySource(SceneItemSpawnEntry entry, Transform spawnPoint)
        {
            switch (entry.Kind)
            {
                case SpawnItemKind.MixTool:
                    SpawnMixToolSource(entry, spawnPoint);
                    break;

                case SpawnItemKind.BasicMaterial:
                    SpawnBasicMaterialSource(entry, spawnPoint);
                    break;
            }
        }

        private void SpawnMixToolSource(SceneItemSpawnEntry entry, Transform spawnPoint)
        {
            MixToolSource prefab = entry.SourcePrefabOverride != null ? entry.SourcePrefabOverride : _mixToolPrefab;
            MixToolSource spawnedToolSource = CreateRoomSourceObject(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation,
                new object[] { (int)entry.ToolType });

            spawnedToolSource.name = $"{entry.GetDefaultName()}Source";
            _spawnedObjects.Add(spawnedToolSource.gameObject);
        }

        private void SpawnBasicMaterialSource(SceneItemSpawnEntry entry, Transform spawnPoint)
        {
            BasicMaterialSource spawnedMaterialSource = CreateRoomSourceObject(
                _basicMaterialPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                new object[] { (int)entry.MaterialType });

            spawnedMaterialSource.name = $"{entry.GetDefaultName()}Source";
            _spawnedObjects.Add(spawnedMaterialSource.gameObject);
        }

        private void SpawnTraySource(Transform spawnPoint, int trayIndex)
        {
            TraySource spawnedTraySource = CreateRoomSourceObject(
                _traySourcePrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                null);

            spawnedTraySource.name = $"TraySource_{trayIndex}";
            _spawnedObjects.Add(spawnedTraySource.gameObject);
        }

        private TSource CreateRoomSourceObject<TSource>(TSource prefab, Vector3 position, Quaternion rotation, object[] instantiationData)
            where TSource : MonoBehaviour
        {
            if (PhotonNetwork.InRoom)
            {
                GameObject spawnedObject = PhotonNetwork.InstantiateRoomObject(
                    prefab.name,
                    position,
                    rotation,
                    0,
                    instantiationData);

                return spawnedObject.GetComponent<TSource>();
            }

            TSource spawnedSource = Instantiate(prefab, position, rotation);
            if (_spawnedItemParent != null)
            {
                spawnedSource.transform.SetParent(_spawnedItemParent, true);
            }

            return spawnedSource;
        }

        private bool HasExistingSpawnSources()
        {
            return FindObjectsOfType<MixToolSource>().Length > 0 ||
                   FindObjectsOfType<BasicMaterialSource>().Length > 0 ||
                   FindObjectsOfType<TraySource>().Length > 0;
        }

        private static void ShuffleEntries(List<SceneItemSpawnEntry> entries)
        {
            System.Random random = new();

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
