using UnityEngine;
using UnityEngine.Serialization;

namespace DontDillyDally.Data
{
    [CreateAssetMenu(fileName = "NewCraftingRule", menuName = "DontDillyDally/Crafting Rule")]
    public class CraftingRuleSO : ScriptableObject
    {
        [Header("Input")]
        [Tooltip("Required tool mask. Dual-input rules store both tools with bitwise OR.")]
        [FormerlySerializedAs("requiredTool")]
        public ToolType RequiredTool;

        [Tooltip("Required action for this rule.")]
        [FormerlySerializedAs("requiredAction")]
        public ActionType RequiredAction;

        [Header("Output")]
        [Tooltip("Result material produced by this rule.")]
        [FormerlySerializedAs("resultMaterial")]
        public CraftedMaterialType ResultMaterial;

        [Header("Timing")]
        [Tooltip("Crafting time in seconds. 0 means instant.")]
        [FormerlySerializedAs("craftingDuration")]
        public float CraftingDuration;

        [Header("Presentation")]
        [Tooltip("Display name for the result.")]
        [FormerlySerializedAs("displayName")]
        public string DisplayName;

        [Tooltip("Optional prefab path for the result.")]
        [FormerlySerializedAs("prefabPath")]
        public string PrefabPath;

        public bool IsMatch(ToolType tool, ActionType action)
        {
            if (RequiredAction == ActionType.None)
                return tool == RequiredTool && action == ActionType.None;

            return tool == RequiredTool && action == RequiredAction;
        }

        public bool IsMatchDual(ToolType tool1, ToolType tool2, ActionType action)
        {
            ToolType requiredMask = tool1 | tool2;
            if (requiredMask == ToolType.None)
                return false;

            return RequiredTool == requiredMask && action == RequiredAction;
        }
    }
}
