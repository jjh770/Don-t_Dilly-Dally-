using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    /// <summary>
    /// CraftedMaterialType → Sprite, ActionType → Sprite 매핑 테이블입니다.
    /// ScriptableObject로 만들어 Inspector에서 아이콘을 등록합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MaterialIconTable", menuName = "DontDillyDally/Material Icon Table")]
    public class MaterialIconTable : ScriptableObject
    {
        [Header("재료 아이콘")]
        [SerializeField] private List<MaterialIconEntry> _materialEntries = new List<MaterialIconEntry>();
        [SerializeField] private Sprite _fallbackMaterialIcon;

        [Header("액션 아이콘")]
        [SerializeField] private List<ActionIconEntry> _actionEntries = new List<ActionIconEntry>();
        [SerializeField] private Sprite _fallbackActionIcon;

        private Dictionary<CraftedMaterialType, MaterialIconEntry> _materialCache;
        private Dictionary<ActionType, Sprite> _actionCache;

        /// <summary>재료 아이콘을 반환합니다.</summary>
        public Sprite GetMaterialIcon(CraftedMaterialType type)
        {
            BuildMaterialCacheIfNeeded();

            if (_materialCache.TryGetValue(type, out MaterialIconEntry entry) && entry.Icon != null)
            {
                return entry.Icon;
            }

            return _fallbackMaterialIcon;
        }

        /// <summary>해당 재료를 만들기 위해 필요한 액션 타입을 반환합니다.</summary>
        public ActionType GetRequiredAction(CraftedMaterialType type)
        {
            BuildMaterialCacheIfNeeded();

            if (_materialCache.TryGetValue(type, out MaterialIconEntry entry))
            {
                return entry.RequiredAction;
            }

            return ActionType.None;
        }

        public Sprite GetActionIcon(ActionType type)
        {
            BuildActionCacheIfNeeded();

            if (_actionCache.TryGetValue(type, out Sprite icon) && icon != null)
            {
                return icon;
            }

            return _fallbackActionIcon;
        }

        public Sprite GetFallbackMaterialIcon()
        {
            return _fallbackMaterialIcon;
        }

        private void BuildMaterialCacheIfNeeded()
        {
            if (_materialCache != null)
            {
                return;
            }

            _materialCache = new Dictionary<CraftedMaterialType, MaterialIconEntry>(_materialEntries.Count);
            for (int i = 0; i < _materialEntries.Count; i++)
            {
                _materialCache[_materialEntries[i].MaterialType] = _materialEntries[i];
            }
        }

        private void BuildActionCacheIfNeeded()
        {
            if (_actionCache != null)
            {
                return;
            }

            _actionCache = new Dictionary<ActionType, Sprite>(_actionEntries.Count);
            for (int i = 0; i < _actionEntries.Count; i++)
            {
                ActionIconEntry entry = _actionEntries[i];
                if (entry.Icon != null)
                {
                    _actionCache[entry.ActionType] = entry.Icon;
                }
            }
        }

        private void OnValidate()
        {
            _materialCache = null;
            _actionCache = null;
        }

        [Serializable]
        public struct MaterialIconEntry
        {
            public CraftedMaterialType MaterialType;
            public Sprite Icon;
            [Tooltip("이 재료를 만들기 위해 필요한 액션 (없으면 None)")]
            public ActionType RequiredAction;
        }

        [Serializable]
        public struct ActionIconEntry
        {
            public ActionType ActionType;
            public Sprite Icon;
        }
    }
}
