using UnityEngine;
using UnityEngine.Serialization;

namespace DontDillyDally.Data
{
    [CreateAssetMenu(fileName = "NewCraftingRule", menuName = "DontDillyDally/Crafting Rule")]
    public class CraftingRuleSO : ScriptableObject
    {
        [Header("입력")]
        [Tooltip("이 규칙에 필요한 도구 마스크입니다. 두 개 입력 규칙은 두 도구를 비트 OR로 함께 저장합니다.")]
        [FormerlySerializedAs("requiredTool")]
        public ToolType RequiredTool;

        [Tooltip("이 규칙에 필요한 행동 타입입니다.")]
        [FormerlySerializedAs("requiredAction")]
        public ActionType RequiredAction;

        [Header("출력")]
        [Tooltip("이 규칙으로 만들어지는 결과 재료입니다.")]
        [FormerlySerializedAs("resultMaterial")]
        public CraftedMaterialType ResultMaterial;

        [Header("시간")]
        [Tooltip("제작에 걸리는 시간(초)입니다. 0이면 즉시 완료됩니다.")]
        [FormerlySerializedAs("craftingDuration")]
        public float CraftingDuration;

        public bool IsMatch(ToolType tool, ActionType action)
        {
            if (RequiredAction == ActionType.None)
                return tool == RequiredTool && action == ActionType.None;

            return tool == RequiredTool && action == RequiredAction;
        }

    }
}
