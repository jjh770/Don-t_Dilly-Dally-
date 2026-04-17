using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 조합 도구의 표시 이름과 모델 프리팹을 연결하는 항목입니다.
    [Serializable]
    public class MixToolPresentationEntry
    {
        [Tooltip("표시 정보를 연결할 조합 도구 타입")]
        public ToolType ToolType = ToolType.None;

        [Tooltip("인스펙터와 UI에서 사용할 표시 이름")]
        public string DisplayName;

        [Tooltip("해당 조합 도구에 사용할 모델 프리팹")]
        public GameObject ModelPrefab;
    }

    // 기본 재료의 표시 이름과 모델 프리팹을 연결하는 항목입니다.
    [Serializable]
    public class BasicMaterialPresentationEntry
    {
        [Tooltip("표시 정보를 연결할 기본 재료 타입")]
        public CraftedMaterialType MaterialType = CraftedMaterialType.None;

        [Tooltip("인스펙터와 UI에서 사용할 표시 이름")]
        public string DisplayName;

        [Tooltip("해당 기본 재료에 사용할 모델 프리팹")]
        public GameObject ModelPrefab;

        public Material[] OverrideMaterials;
    }

    // 타입별 표시 이름과 모델 프리팹을 주입하기 위한 카탈로그입니다.
    [CreateAssetMenu(fileName = "ItemPresentationCatalog", menuName = "DontDillyDally/Item Presentation Catalog")]
    public class ItemPresentationCatalog : ScriptableObject
    {
        [Header("조합 도구 표시 정보")]
        [Tooltip("ToolType별 표시 이름과 모델 프리팹 목록")]
        public List<MixToolPresentationEntry> MixToolEntries = new List<MixToolPresentationEntry>();

        [Header("기본 재료 표시 정보")]
        [Tooltip("CraftedMaterialType별 표시 이름과 모델 프리팹 목록")]
        public List<BasicMaterialPresentationEntry> BasicMaterialEntries = new List<BasicMaterialPresentationEntry>();

        public bool TryGetMixToolPresentation(
            ToolType toolType,
            out string displayName,
            out GameObject modelPrefab)
        {
            foreach (MixToolPresentationEntry entry in MixToolEntries)
            {
                if (entry == null || entry.ToolType != toolType)
                    continue;

                displayName = entry.DisplayName;
                modelPrefab = entry.ModelPrefab;
                return true;
            }

            displayName = null;
            modelPrefab = null;
            return false;
        }

        public bool TryGetBasicMaterialPresentation(
            CraftedMaterialType materialType,
            out string displayName,
            out GameObject modelPrefab,
            out Material[] overrideMaterials)
        {
            foreach (BasicMaterialPresentationEntry entry in BasicMaterialEntries)
            {
                if (entry == null || entry.MaterialType != materialType)
                    continue;

                displayName = entry.DisplayName;
                modelPrefab = entry.ModelPrefab;
                overrideMaterials = entry.OverrideMaterials;
                return true;
            }

            displayName = null;
            modelPrefab = null;
            overrideMaterials = null;
            return false;
        }
    }
}