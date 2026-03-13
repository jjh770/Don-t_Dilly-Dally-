using UnityEngine;
using UnityEngine.Serialization;

namespace DontDillyDally.Data
{
    // 하나의 조합 규칙을 정의하는 ScriptableObject입니다.
    // 어떤 도구와 행동으로 어떤 결과물이 나오는지 데이터로 관리합니다.
    [CreateAssetMenu(fileName = "NewCraftingRule", menuName = "DontDillyDally/Crafting Rule")]
    public class CraftingRuleSO : ScriptableObject
    {
        [Header("입력 조건")]
        [Tooltip("조합에 필요한 기본 도구")]
        [FormerlySerializedAs("requiredTool")]
        public ToolType RequiredTool;

        [Tooltip("조합에 필요한 행동. None이면 도구만으로 완성된다.")]
        [FormerlySerializedAs("requiredAction")]
        public ActionType RequiredAction;

        [Header("추가 입력")]
        [Tooltip("두 번째 도구가 필요한 경우 사용한다. 예: 물약 혼합")]
        [FormerlySerializedAs("secondaryTool")]
        public ToolType SecondaryTool;

        [Header("출력 결과")]
        [Tooltip("조합이 성공했을 때 생성되는 결과 재료")]
        [FormerlySerializedAs("resultMaterial")]
        public CraftedMaterialType ResultMaterial;

        [Header("시간 정보")]
        [Tooltip("조합에 걸리는 시간(초). 0이면 즉시 완성")]
        [FormerlySerializedAs("craftingDuration")]
        public float CraftingDuration;

        [Header("표시 정보")]
        [Tooltip("결과물 표시 이름")]
        [FormerlySerializedAs("displayName")]
        public string DisplayName;

        [Tooltip("결과물 프리팹 경로")]
        [FormerlySerializedAs("prefabPath")]
        public string PrefabPath;

        // 단일 도구 조합 규칙과 입력값이 일치하는지 확인합니다.
        public bool IsMatch(ToolType tool, ActionType action)
        {
            if (RequiredAction == ActionType.None)
            {
                return tool == RequiredTool && action == ActionType.None;
            }

            return tool == RequiredTool && action == RequiredAction;
        }

        // 두 개의 도구를 사용하는 조합 규칙과 입력값이 일치하는지 확인합니다.
        public bool IsMatchDual(ToolType tool1, ToolType tool2, ActionType action)
        {
            if (SecondaryTool == ToolType.None)
                return false;

            bool forward = tool1 == RequiredTool && tool2 == SecondaryTool;
            bool reverse = tool1 == SecondaryTool && tool2 == RequiredTool;

            return (forward || reverse) && action == RequiredAction;
        }
    }
}
