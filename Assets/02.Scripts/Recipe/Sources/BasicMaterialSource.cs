using UnityEngine;

namespace DontDillyDally.Data
{
    // 기본 재료를 무한히 공급하는 공급원입니다.
    // 재료 타입만 설정하면 공통 공급원 로직을 통해 아이템을 생성하고 유지합니다.
    public class BasicMaterialSource : ItemSource<BasicMaterialItem>
    {
        [Header("기본 재료 공급원")]
        [Tooltip("이 공급원이 생성할 기본 재료 타입")]
        public CraftedMaterialType MaterialType = CraftedMaterialType.None;

        public void Initialize(CraftedMaterialType materialType)
        {
            MaterialType = materialType;
            ForceRespawn();
        }

        public void SetMaterialType(CraftedMaterialType materialType)
        {
            MaterialType = materialType;
            ForceRespawn();
        }

        protected override bool CanSpawnItem()
        {
            return base.CanSpawnItem() && MaterialType != CraftedMaterialType.None;
        }

        protected override object[] GetInstantiationData()
        {
            return new object[] { (int)MaterialType };
        }

        protected override string GetDefaultItemName()
        {
            return MaterialType.ToString();
        }

        protected override void ApplySourceStateFromInstantiationData(object[] data)
        {
            if (data == null || data.Length == 0)
                return;

            if (data[0] is int materialTypeValue)
            {
                MaterialType = (CraftedMaterialType)materialTypeValue;
            }
        }
    }
}
