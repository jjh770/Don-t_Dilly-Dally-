using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public class StageFlowBootstrapper : MonoBehaviour
    {
        [Header("스테이지 설정")]
        [SerializeField] private StageCatalogSO _stageCatalog;
        [SerializeField] private int _stageIndex;

        private void Start()
        {
            if (_stageCatalog == null)
            {
                Debug.LogError("[StageFlowBootstrapper] StageCatalogSO가 연결되지 않았습니다.");
                return;
            }

            if (!_stageCatalog.TryGetStageDefinition(_stageIndex, out StageDefinitionSO stageDefinition))
            {
                Debug.LogError($"[StageFlowBootstrapper] StageCatalog에서 인덱스 {_stageIndex}에 해당하는 StageDefinitionSO를 찾지 못했습니다.");
                return;
            }

            // 카탈로그에서 선택한 스테이지 정의를 런타임 데이터로 변환해서 실제 게임에 넘깁니다.
            StageRuntimeData stageData = stageDefinition.CreateRuntimeData();
            StageFlowManager.Instance.Initialize(stageData);
        }
    }
}
