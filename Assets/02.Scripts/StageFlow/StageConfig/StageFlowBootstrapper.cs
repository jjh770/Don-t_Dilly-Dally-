using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

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

        private StageRuntimeData _stageData;
        private GameObject _stagePrefab;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (RoomDataManager.Instance == null) 
            {
                Debug.LogWarning("[StageFlowBootstrapper] RoomDataManager 인스턴스가 아직 준비되지 않았습니다.");
                return;
            }

            var stageSceneDef = RoomDataManager.Instance.CurrentStageSceneDefinition;

            if (stageSceneDef == null)
            {
                Debug.LogError($"[StageFlowBootstrapper] Stage Definition을 받아오지 못했습니다.");
                return;
            }

            _stageData = stageSceneDef.CreateRuntimeData();
            _stagePrefab = stageSceneDef.StagePrefab;

            if (StagePreloader.Instance == null)
            {
                Debug.LogError("[StageFlowBootstrapper] StagePreloader 인스턴스가 아직 준비되지 않았습니다.");
                return;
            }

            StagePreloader.Instance.Initialize(_stageData);
            SceneLoadManager.Instance.OnSceneLoadComplete += HandleSceneLoadComplete;
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

                // 마스터만 컷씬 중 생성한 데이터 준비 완료를 기다립니다.
                if (StagePreloader.Instance != null)
                {
                    PhotonNetwork.Instantiate(_stagePrefab.name, Vector3.zero, Quaternion.identity);
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
            StagePreloader.Instance?.Cleanup();
        }

        private void Cleanup()
        {
            if (SceneLoadManager.Instance != null)
            {
                SceneLoadManager.Instance.OnSceneLoadComplete -= HandleSceneLoadComplete;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
