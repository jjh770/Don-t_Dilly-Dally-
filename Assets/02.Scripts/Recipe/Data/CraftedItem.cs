using System;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 제작 전후의 조합 결과를 표현하는 아이템 데이터입니다.
    // 어떤 도구와 행동으로 준비되었는지 함께 추적합니다.
    [Serializable]
    public class CraftedItem
    {
        public CraftedMaterialType MaterialType;
        public ToolType UsedTool;
        public ActionType UsedAction;
        public ToolType SecondaryTool;
        public float PreparedTime;
        public int PreparedByPlayerId;

        public bool MatchesMaterial(CraftedMaterialType required)
        {
            return MaterialType == required;
        }

        // 추가 가공 없이 바로 사용할 수 있는 기본 재료를 CraftedItem으로 감쌉니다.
        public static CraftedItem CreateBasicMaterial(
            CraftedMaterialType materialType,
            int playerId = 0)
        {
            if (!IsBasicMaterial(materialType))
            {
                Debug.LogWarning($"[CraftedItem] '{materialType}'은 기본 재료 생성 대상이 아닙니다.");
                return null;
            }

            return new CraftedItem
            {
                MaterialType = materialType,
                UsedTool = ToolType.None,
                UsedAction = ActionType.None,
                SecondaryTool = ToolType.None,
                PreparedTime = Time.time,
                PreparedByPlayerId = playerId
            };
        }

        public static bool IsBasicMaterial(CraftedMaterialType materialType)
        {
            return materialType.IsBasicMaterial();
        }
    }
}
