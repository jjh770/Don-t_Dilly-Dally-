using UnityEngine;

namespace DontDillyDally.Data
{
    // 공급원이 생성하는 실제 기본 재료 아이템입니다.
    // 공통 Item 프리팹에서 타입에 맞는 모델, 표시 이름, 콜라이더를 자동으로 주입받습니다.
    public class BasicMaterialItem : ItemObject
    {
        [Header("실제 재료 정보")]
        [Tooltip("이 아이템이 나타내는 실제 기본 재료 타입")]
        public CraftedMaterialType MaterialType = CraftedMaterialType.None;

        [Tooltip("타입별 표시 이름과 모델을 자동으로 찾아올 카탈로그")]
        public ItemPresentationCatalog PresentationCatalog;

        public void Initialize(CraftedMaterialType materialType)
        {
            MaterialType = materialType;

            PresentationResolver<CraftedMaterialType> resolver =
                PresentationCatalog != null
                    ? PresentationCatalog.TryGetBasicMaterialPresentation
                    : null;

            InitializeWithPresentation(
                materialType,
                materialType.ToString(),
                resolver);
        }

        public CraftedItem CreatePreparedItem(int playerId = 0)
        {
            return CraftedItem.CreateBasicMaterial(MaterialType, playerId);
        }
    }
}
