using Cysharp.Threading.Tasks;
using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontDillyDally.StageFlow
{
    /// <summary>
    /// 컷씬 씬에서 시작되어 Gameplay 씬까지 유지되는 브리지입니다.
    /// Cutscene: StagePreloader를 초기화합니다.
    /// Gameplay: StageFlowManager를 런타임 데이터로 초기화합니다.
    /// WaitingRoom: 자신을 정리하고 파괴합니다.
    /// </summary>
    public class StageFlowBootstrapper : MonoBehaviour
    {
        private const float StageFlowManagerWaitTimeoutSec = 5f;
        private const string WaitingRoomSceneName = "WaitingRoom";

        public static StageFlowBootstrapper Instance { get; private set; }
        public static event Action StageFlowReady;

        public bool IsStageFlowReady { get; private set; }

        private StageRuntimeData _stageData;
        private GameObject _stagePrefab;
        private GameObject _spawnedStageInstance;
        private bool _isCleaningUp;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            IsStageFlowReady = false;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnUnitySceneLoaded;
        }

        private void Start()
        {
            if (RoomDataManager.Instance == null)
            {
                Debug.LogWarning("[StageFlowBootstrapper] RoomDataManager 인스턴스가 아직 준비되지 않았습니다.");
                return;
            }

            _stageData = RoomDataManager.Instance.CreateCurrentStageRuntimeData();
            _stagePrefab = RoomDataManager.Instance.CurrentStagePrefab;

            if (StagePreloader.Instance == null)
            {
                Debug.LogError("[StageFlowBootstrapper] StagePreloader 인스턴스가 아직 준비되지 않았습니다.");
                return;
            }

            if (_stageData == null)
            {
                Debug.LogError("[StageFlowBootstrapper] StageRuntimeData 생성에 실패했습니다.");
                return;
            }

            StagePreloader.Instance.Initialize(_stageData);
            SceneLoadManager.Instance.OnSceneLoadComplete += HandleSceneLoadComplete;
        }

        // 비마스터 클라이언트에서는 WaitingRoom 복귀가 SceneLoadManager.BeginSceneLoad를 거치지
        // 않아 OnSceneLoadComplete(WaitingRoom)이 발화하지 않는다. 누가 씬 전환을 트리거했는지와
        // 무관하게 Unity의 SceneManager.sceneLoaded로 감지해 Cleanup을 보장한다.
        private void OnUnitySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == WaitingRoomSceneName)
            {
                Cleanup();
            }
        }

        private void HandleSceneLoadComplete(ESceneType sceneType)
        {
            if (sceneType == ESceneType.Gameplay)
            {
                InitializeGameplay().Forget();
            }
            else if (sceneType == ESceneType.WaitingRoom)
            {
                Cleanup();
            }
        }

        private async UniTaskVoid InitializeGameplay()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (_stagePrefab == null)
                {
                    Debug.LogError("[StageFlowBootstrapper] Stage Prefab이 설정되지 않았습니다.");
                    return;
                }

                // 마스터는 스테이지 프리팹을 생성하고 데이터 준비 완료까지 보장합니다.
                CleanupSpawnedStageInstance();
                _spawnedStageInstance = PhotonNetwork.Instantiate(_stagePrefab.name, Vector3.zero, Quaternion.identity);

                if (StagePreloader.Instance != null)
                {
                    await StagePreloader.Instance.WaitForDataPrep(destroyCancellationToken);
                }
            }

            float deadline = Time.unscaledTime + StageFlowManagerWaitTimeoutSec;
            while (StageFlowManager.Instance == null &&
                   Time.unscaledTime < deadline &&
                   !destroyCancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(destroyCancellationToken);
            }

            if (StageFlowManager.Instance == null)
            {
                Debug.LogError("[StageFlowBootstrapper] Gameplay 씬에서 StageFlowManager 인스턴스가 준비되지 않았습니다.");
                return;
            }

            StageFlowManager.Instance.Initialize(_stageData);
            IsStageFlowReady = true;
            StageFlowReady?.Invoke();
            StagePreloader.Instance?.Cleanup();
        }

        private void Cleanup()
        {
            if (_isCleaningUp)
            {
                return;
            }

            CleanupSpawnedStageInstance();
            ReleaseResources();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            ReleaseResources();
            SceneManager.sceneLoaded -= OnUnitySceneLoaded;

            if (Instance == this)
            {
                Instance = null;
                IsStageFlowReady = false;
            }
        }

        private void ReleaseResources()
        {
            if (_isCleaningUp)
            {
                return;
            }

            _isCleaningUp = true;
            IsStageFlowReady = false;

            if (SceneLoadManager.Instance != null)
            {
                SceneLoadManager.Instance.OnSceneLoadComplete -= HandleSceneLoadComplete;
            }
        }

        public bool CleanupSpawnedStageInstance()
        {
            if (_spawnedStageInstance == null)
            {
                return false;
            }

            GameObject target = _spawnedStageInstance;
            _spawnedStageInstance = null;

            PhotonView pv = target.GetComponent<PhotonView>();
            if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && pv != null && pv.IsMine)
            {
                PhotonNetwork.Destroy(target);
            }
            else
            {
                Destroy(target);
            }

            return true;
        }
    }
}
