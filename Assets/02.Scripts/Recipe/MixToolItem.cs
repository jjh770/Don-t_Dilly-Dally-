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

            string resolvedDisplayName = string.IsNullOrWhiteSpace(DisplayName)
                ? toolType.ToString()
                : DisplayName;

            GameObject resolvedModelPrefab = ModelPrefab;

            if (PresentationCatalog != null &&
                PresentationCatalog.TryGetMixToolPresentation(
                    toolType,
                    out string catalogDisplayName,
                    out GameObject catalogModelPrefab,
                    out BoxColliderPresentation boxCollider))
            {
                if (!string.IsNullOrWhiteSpace(catalogDisplayName))
                    resolvedDisplayName = catalogDisplayName;

                if (catalogModelPrefab != null)
                    resolvedModelPrefab = catalogModelPrefab;

                if (boxCollider != null && boxCollider.UseOverride)
                    ApplyBoxCollider(boxCollider.Center, boxCollider.Size);
            }

            if (PresentationCatalog == null || !TryApplyBoxColliderOverride(toolType))
                TryApplyBoxColliderFromModelPrefab(resolvedModelPrefab);

            base.Initialize(resolvedDisplayName, resolvedModelPrefab);
        }

        private bool TryApplyBoxColliderOverride(ToolType toolType)
        {
            if (PresentationCatalog == null)
                return false;

            if (!PresentationCatalog.TryGetMixToolPresentation(
                    toolType,
                    out _,
                    out _,
                    out BoxColliderPresentation boxCollider))
            {
                return false;
            }

            if (boxCollider == null || !boxCollider.UseOverride)
                return false;

            ApplyBoxCollider(boxCollider.Center, boxCollider.Size);
            return true;
        }
    }
}
