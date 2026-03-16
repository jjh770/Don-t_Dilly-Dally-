using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 타입별 BoxCollider 값을 저장하는 항목입니다.
    [Serializable]
    public class BoxColliderPresentation
    {
        [Tooltip("이 항목의 BoxCollider 값을 실제로 적용할지 여부")]
        public bool UseOverride = false;

        [Tooltip("적용할 BoxCollider Center 값")]
        public Vector3 Center = Vector3.zero;

        [Tooltip("적용할 BoxCollider Size 값")]
        public Vector3 Size = Vector3.one;
    }

    // 조합 도구의 표시 이름, 모델 프리팹, 콜라이더 값을 연결하는 항목입니다.
    [Serializable]
    public class MixToolPresentationEntry
    {
        [Tooltip("표시 정보를 연결할 조합 도구 타입")]
        public ToolType ToolType = ToolType.None;

        [Tooltip("인스펙터와 씬에서 사용할 표시 이름")]
        public string DisplayName;

        [Tooltip("해당 조합 도구에 사용할 모델 프리팹")]
        public GameObject ModelPrefab;

        [Tooltip("해당 조합 도구에 적용할 BoxCollider 값")]
        public BoxColliderPresentation BoxCollider = new BoxColliderPresentation();
    }

    // 기본 재료의 표시 이름, 모델 프리팹, 콜라이더 값을 연결하는 항목입니다.
    [Serializable]
    public class BasicMaterialPresentationEntry
    {
        [Tooltip("표시 정보를 연결할 기본 재료 타입")]
        public CraftedMaterialType MaterialType = CraftedMaterialType.None;

        [Tooltip("인스펙터와 씬에서 사용할 표시 이름")]
        public string DisplayName;

        [Tooltip("해당 기본 재료에 사용할 모델 프리팹")]
        public GameObject ModelPrefab;

        [Tooltip("해당 기본 재료에 적용할 BoxCollider 값")]
        public BoxColliderPresentation BoxCollider = new BoxColliderPresentation();
    }

    // 공통 Item 프리팹이 타입별 표시 이름, 모델, BoxCollider 값을 자동으로 주입받기 위한 카탈로그입니다.
    [CreateAssetMenu(fileName = "ItemPresentationCatalog", menuName = "DontDillyDally/Item Presentation Catalog")]
    public class ItemPresentationCatalog : ScriptableObject
    {
        [Header("조합 도구 표시 정보")]
        [Tooltip("ToolType별 표시 이름, 모델 프리팹, BoxCollider 값 목록")]
        public List<MixToolPresentationEntry> MixToolEntries = new List<MixToolPresentationEntry>();

        [Header("기본 재료 표시 정보")]
        [Tooltip("CraftedMaterialType별 표시 이름, 모델 프리팹, BoxCollider 값 목록")]
        public List<BasicMaterialPresentationEntry> BasicMaterialEntries = new List<BasicMaterialPresentationEntry>();

        public bool TryGetMixToolPresentation(
            ToolType toolType,
            out string displayName,
            out GameObject modelPrefab,
            out BoxColliderPresentation boxCollider)
        {
            foreach (MixToolPresentationEntry entry in MixToolEntries)
            {
                if (entry == null || entry.ToolType != toolType)
                    continue;

                displayName = entry.DisplayName;
                modelPrefab = entry.ModelPrefab;
                boxCollider = entry.BoxCollider;
                return true;
            }

            displayName = null;
            modelPrefab = null;
            boxCollider = null;
            return false;
        }

        public bool TryGetBasicMaterialPresentation(
            CraftedMaterialType materialType,
            out string displayName,
            out GameObject modelPrefab,
            out BoxColliderPresentation boxCollider)
        {
            foreach (BasicMaterialPresentationEntry entry in BasicMaterialEntries)
            {
                if (entry == null || entry.MaterialType != materialType)
                    continue;

                displayName = entry.DisplayName;
                modelPrefab = entry.ModelPrefab;
                boxCollider = entry.BoxCollider;
                return true;
            }

            displayName = null;
            modelPrefab = null;
            boxCollider = null;
            return false;
        }
    }
}
