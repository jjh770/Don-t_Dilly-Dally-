using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 공급원이 생성하는 실제 기본 재료 아이템입니다.
    // 공통 Item 프리팹에서 타입에 맞는 모델, 표시 이름, 콜라이더를 자동으로 주입받습니다.
    public class BasicMaterialItem : ItemObject, IPunInstantiateMagicCallback
    {
        [Header("실제 재료 정보")]
        [Tooltip("이 아이템이 나타내는 실제 기본 재료 타입")]
        public CraftedMaterialType MaterialType = CraftedMaterialType.None;

        [Tooltip("타입별 표시 이름과 모델을 자동으로 찾아올 카탈로그")]
        public ItemPresentationCatalog PresentationCatalog;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize(CraftedMaterialType materialType)
        {
            ResetReusableItemState();
            ResetSourceState();
            SetAsSupplyItem();

            MaterialType = materialType;

            string resolvedDisplayName = materialType.ToString();
            GameObject resolvedModelPrefab = ModelPrefab;
            Material[] overrideMaterials = null;

            if (PresentationCatalog != null &&
                PresentationCatalog.TryGetBasicMaterialPresentation(
                    materialType,
                    out string catalogDisplayName,
                    out GameObject catalogModelPrefab,
                    out Material[] catalogOverrideMaterials))
            {
                if (!string.IsNullOrWhiteSpace(catalogDisplayName))
                {
                    resolvedDisplayName = catalogDisplayName;
                }

                if (catalogModelPrefab != null)
                {
                    resolvedModelPrefab = catalogModelPrefab;
                }

                overrideMaterials = catalogOverrideMaterials;
            }

            Initialize(resolvedDisplayName, resolvedModelPrefab);
            ApplyBoxColliderFromPresentationModel(resolvedModelPrefab);
            ApplyOverrideMaterials(overrideMaterials);
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            PhotonView photonView = info.photonView;
            object[] data = photonView.InstantiationData;

            if (data == null || data.Length == 0)
            {
                Debug.Log("[BasicMaterialItem] 인스턴스화 데이터가 없습니다.");
                return;
            }

            if (data[0] is not int materialTypeValue)
            {
                Debug.Log("[BasicMaterialItem] 인스턴스화 데이터에서 재료 타입을 찾을 수 없습니다.");
                return;
            }

            CraftedMaterialType materialType = (CraftedMaterialType)materialTypeValue;
            Initialize(materialType);
        }

        private void ApplyBoxColliderFromPresentationModel(GameObject modelPrefab)
        {
            if (modelPrefab == null)
            {
                return;
            }

            TryApplyBoxColliderFromModelPrefab(modelPrefab);
        }
    }
}