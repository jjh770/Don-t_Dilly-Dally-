using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [CreateAssetMenu(fileName = "StageCatalog", menuName = "DontDillyDally/Stage Flow/Stage Catalog")]
    public class StageCatalogSO : ScriptableObject
    {
        [Header("스테이지 목록")]
        [SerializeField] private List<StageDefinitionSO> _stageDefinitions = new();

        public IReadOnlyList<StageDefinitionSO> StageDefinitions => _stageDefinitions;
        public int StageCount => _stageDefinitions != null ? _stageDefinitions.Count : 0;

        // 인덱스로 스테이지 정의를 안전하게 가져옵니다.
        public bool TryGetStageDefinition(int stageIndex, out StageDefinitionSO stageDefinition)
        {
            stageDefinition = null;

            if (_stageDefinitions == null || stageIndex < 0 || stageIndex >= _stageDefinitions.Count)
            {
                return false;
            }

            stageDefinition = _stageDefinitions[stageIndex];
            return stageDefinition != null;
        }
    }
}
