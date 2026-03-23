using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 씬에 배치할 일반 공급원 종류를 구분합니다.
    public enum SpawnItemKind
    {
        None = 0,
        MixTool,
        BasicMaterial
    }

    // 씬 스폰에 사용할 일반 아이템 배치 정보 1개입니다.
    [Serializable]
    public class SceneItemSpawnEntry
    {
        [Tooltip("이 항목이 조합 도구 공급원인지, 기본 재료 공급원인지 구분합니다.")]
        public SpawnItemKind Kind = SpawnItemKind.None;

        [Tooltip("조합 도구 공급원일 때 사용할 ToolType입니다.")]
        public ToolType ToolType = ToolType.None;

        [Tooltip("기본 재료 공급원일 때 사용할 CraftedMaterialType입니다.")]
        public CraftedMaterialType MaterialType = CraftedMaterialType.None;

        [Tooltip("기본 공급원 프리팹 대신 사용할 전용 프리팹입니다. 비어 있으면 SceneItemSpawner의 기본 프리팹을 사용합니다.")]
        public MixToolSource SourcePrefabOverride;

        public bool IsValid()
        {
            switch (Kind)
            {
                case SpawnItemKind.MixTool:
                    return ToolType != ToolType.None;

                case SpawnItemKind.BasicMaterial:
                    return MaterialType != CraftedMaterialType.None &&
                           MaterialType.IsBasicMaterial();

                default:
                    return false;
            }
        }

        public string GetIdentityKey()
        {
            switch (Kind)
            {
                case SpawnItemKind.MixTool:
                    return $"Tool:{ToolType}";

                case SpawnItemKind.BasicMaterial:
                    return $"Material:{MaterialType}";

                default:
                    return "Invalid";
            }
        }

        public string GetDefaultName()
        {
            switch (Kind)
            {
                case SpawnItemKind.MixTool:
                    return ToolType.ToString();

                case SpawnItemKind.BasicMaterial:
                    return MaterialType.ToString();

                default:
                    return "Invalid";
            }
        }
    }

    // 씬 시작 시 중복 없이 배치할 일반 공급원 후보 목록을 담는 카탈로그입니다.
    [CreateAssetMenu(fileName = "SceneItemSpawnCatalog", menuName = "DontDillyDally/Scene Item Spawn Catalog")]
    public class SceneItemSpawnCatalog : ScriptableObject
    {
        [Header("배치 후보 목록")]
        [Tooltip("씬에 중복 없이 배치할 조합 도구 공급원과 기본 재료 공급원 목록")]
        public List<SceneItemSpawnEntry> Entries = new List<SceneItemSpawnEntry>();

        public List<SceneItemSpawnEntry> GetValidEntries()
        {
            return Entries
                .Where(entry => entry != null && entry.IsValid())
                .ToList();
        }

        public bool HasKind(SpawnItemKind kind)
        {
            return Entries.Any(entry => entry != null && entry.IsValid() && entry.Kind == kind);
        }

        public bool Validate()
        {
            if (Entries == null || Entries.Count == 0)
            {
                Debug.LogWarning("[SceneItemSpawnCatalog] 배치 후보가 비어 있습니다.");
                return false;
            }

            List<SceneItemSpawnEntry> validEntries = GetValidEntries();
            if (validEntries.Count == 0)
            {
                Debug.LogWarning("[SceneItemSpawnCatalog] 유효한 배치 후보가 없습니다.");
                return false;
            }

            HashSet<string> identityKeys = new HashSet<string>();

            foreach (SceneItemSpawnEntry entry in validEntries)
            {
                string identityKey = entry.GetIdentityKey();
                if (!identityKeys.Add(identityKey))
                {
                    Debug.LogWarning($"[SceneItemSpawnCatalog] 중복된 배치 후보가 있습니다. ({identityKey})");
                    return false;
                }
            }

            return true;
        }
    }
}
