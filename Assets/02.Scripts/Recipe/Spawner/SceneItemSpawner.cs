using DontDillyDally.StageFlow;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
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

        [Header("실행 설정")]
        [Tooltip("씬 시작 시 자동으로 일반 공급원을 배치할지 여부")]
        [SerializeField] private bool _spawnItemsOnStart = true;

        [Tooltip("씬 시작 시 자동으로 트레이를 배치할지 여부")]
        [SerializeField] private bool _spawnTraysOnStart = true;

        [Tooltip("다시 배치하기 전에 기존 생성 오브젝트를 먼저 지울지 여부")]
        [SerializeField] private bool _clearBeforeSpawn = true;

        private readonly List<GameObject> _spawnedObjects = new();

        private bool _hasSpawnedSceneObjects;

        private bool _isSubscribed;

        private void Start()
        {
            TrySubscribe();
        }

        private void Update()
        {
            if (!_isSubscribed)
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

            if (StageFlowManager.Instance.CurrentStageData != null)
                HandleStageDataChanged(StageFlowManager.Instance.CurrentStageData);
        }

        private void HandleStageDataChanged(StageRuntimeData stageData)
        {
            if (stageData == null)
                return;

            ClearSpawnedItems();
            _hasSpawnedSceneObjects = false;

            TryInitializeSpawnLayout();
        }

        private bool ValidateSceneConfig()
        {
            StageSceneConfig sceneConfig = StageSceneConfig.Instance;
            if (sceneConfig == null)
            {
                Debug.LogWarning("[SceneItemSpawner] StageSceneConfig 인스턴스가 없습니다.");
                return false;
            }

            if (_spawnItemsOnStart)
            {
                if (sceneConfig.ItemSpawnCatalog == null)
                {
                    Debug.LogWarning($"[SceneItemSpawner] 스테이지 '{sceneConfig.StageId}'의 ItemSpawnCatalog가 비어 있습니다.");
                    return false;
                }

                if (!sceneConfig.ItemSpawnCatalog.Validate())
                    return false;

                if (sceneConfig.ItemSpawnPoints == null || sceneConfig.ItemSpawnPoints.Count == 0)
                {
                    Debug.LogWarning($"[SceneItemSpawner] 스테이지 '{sceneConfig.StageId}'의 ItemSpawnPoints가 비어 있습니다.");
                    return false;
                }

                if (sceneConfig.ItemSpawnCatalog.HasKind(SpawnItemKind.MixTool) && _mixToolPrefab == null)
                {
                    Debug.LogWarning("[SceneItemSpawner] MixToolSource 프리팹이 연결되지 않았습니다.");
                    return false;
                }

                if (sceneConfig.ItemSpawnCatalog.HasKind(SpawnItemKind.BasicMaterial) && _basicMaterialPrefab == null)
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

                if (sceneConfig.TraySpawnPoints == null || sceneConfig.TraySpawnPoints.Count == 0)
                {
                    Debug.LogWarning($"[SceneItemSpawner] 스테이지 '{sceneConfig.StageId}'의 TraySpawnPoints가 비어 있습니다.");
                    return false;
                }
            }

            return true;
        }

        public override void OnJoinedRoom()
        {
            TryInitializeSpawnLayout();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
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

            StageSceneConfig sceneConfig = StageSceneConfig.Instance;
            if (sceneConfig == null || sceneConfig.TraySpawnPoints == null || sceneConfig.TraySpawnPoints.Count == 0)
            {
                Debug.LogWarning("[SceneItemSpawner] TraySpawnPoints가 비어 있습니다.");
                return;
            }

            for (int i = 0; i < sceneConfig.TraySpawnPoints.Count; i++)
            {
                Transform spawnPoint = sceneConfig.TraySpawnPoints[i];
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
                if (spawnedObject == null)
                {
                    continue;
                }

                if (PhotonNetwork.InRoom)
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonView view = spawnedObject.GetComponent<PhotonView>();
                        if (view != null && view.ViewID > 0)
                        {
                            PhotonNetwork.Destroy(spawnedObject);
                        }
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

            if (!ValidateSceneConfig())
                return;

            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
                return;

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
            List<Transform> spawnPoints = StageSceneConfig.Instance.ItemSpawnPoints;
            int spawnCount = Mathf.Min(entries.Count, spawnPoints.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                SceneItemSpawnEntry entry = entries[i];
                Transform spawnPoint = spawnPoints[i];

                if (spawnPoint == null)
                    continue;

                SpawnEntrySource(entry, spawnPoint);
            }

            if (spawnPoints.Count < entries.Count)
            {
                Debug.LogWarning(
                    $"[SceneItemSpawner] 일반 아이템 스폰 위치가 부족해 {entries.Count - spawnPoints.Count}개를 배치하지 못했습니다.");
            }
        }

        private List<SceneItemSpawnEntry> CreateShuffledEntries()
        {
            List<SceneItemSpawnEntry> entries = StageSceneConfig.Instance.ItemSpawnCatalog.GetValidEntries();
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
            if (spawnedToolSource == null)
            {
                return;
            }

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
            if (spawnedMaterialSource == null)
            {
                return;
            }

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
            if (spawnedTraySource == null)
            {
                return;
            }

            spawnedTraySource.name = $"TraySource_{trayIndex}";
            _spawnedObjects.Add(spawnedTraySource.gameObject);
        }

        private TSource CreateRoomSourceObject<TSource>(TSource prefab, Vector3 position, Quaternion rotation, object[] instantiationData)
            where TSource : MonoBehaviour
        {
            if (!PhotonNetwork.InRoom)
            {
                return null;
            }

            GameObject spawnedObject = PhotonNetwork.InstantiateRoomObject(
                prefab.name,
                position,
                rotation,
                0,
                instantiationData);

            return spawnedObject.GetComponent<TSource>();
        }

        private bool HasExistingSpawnSources()
        {
            return FindAnyObjectByType<MixToolSource>() != null ||
                   FindAnyObjectByType<BasicMaterialSource>() != null ||
                   FindAnyObjectByType<TraySource>() != null;
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
