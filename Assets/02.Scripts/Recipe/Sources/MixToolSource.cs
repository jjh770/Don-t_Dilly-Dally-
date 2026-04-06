using UnityEngine;

namespace DontDillyDally.Data
{
    // 조합 도구를 무한히 공급하는 공급원입니다.
    // 도구 타입만 설정하면 공통 공급원 로직을 통해 아이템을 생성하고 유지합니다.
    public class MixToolSource : ItemSource<MixToolItem>
    {
        [Header("조합 도구 공급원")]
        [Tooltip("이 공급원이 생성할 조합 도구 타입")]
        public ToolType ToolType = ToolType.None;

        public void Initialize(ToolType toolType)
        {
            ToolType = toolType;
            ForceRespawn();
        }

        public void SetToolType(ToolType toolType)
        {
            ToolType = toolType;
            ForceRespawn();
        }

        protected override bool CanSpawnItem()
        {
            return base.CanSpawnItem() && ToolType != ToolType.None;
        }

        protected override string GetDefaultItemName()
        {
            return ToolType.ToString();
        }

        protected override object[] GetInstantiationData()
        {
            return new object[] { (int)ToolType };
        }

        protected override void ApplySourceStateFromInstantiationData(object[] data)
        {
            if (data == null || data.Length == 0)
                return;

            if (data[0] is int toolTypeValue)
            {
                ToolType = (ToolType)toolTypeValue;
            }
        }
    }
}
