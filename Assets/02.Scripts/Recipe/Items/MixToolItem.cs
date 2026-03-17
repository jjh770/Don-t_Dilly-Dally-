using UnityEngine;

namespace DontDillyDally.Data
{
    // 공급원이 생성하는 실제 조합 도구 아이템입니다.
    // 공통 Item 프리팹에서 타입에 맞는 모델, 표시 이름, 콜라이더를 자동으로 주입받습니다.
    public class MixToolItem : ItemObject
    {
        [Header("실제 도구 정보")]
        [Tooltip("이 아이템이 나타내는 실제 조합 도구 타입")]
        public ToolType ToolType = ToolType.None;

        [Tooltip("타입별 표시 이름과 모델을 자동으로 찾아올 카탈로그")]
        public ItemPresentationCatalog PresentationCatalog;

        public void Initialize(ToolType toolType)
        {
            ToolType = toolType;

            PresentationResolver<ToolType> resolver =
                PresentationCatalog != null
                    ? PresentationCatalog.TryGetMixToolPresentation
                    : null;

            InitializeWithPresentation(
                toolType,
                toolType.ToString(),
                resolver);
        }
    }
}
