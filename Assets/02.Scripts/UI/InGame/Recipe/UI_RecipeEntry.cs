using DontDillyDally.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.UI
{
    /// <summary>
    /// 레시피 1개 단위 UI입니다.
    /// TitleFrame과 RecipeItems를 포함합니다.
    /// </summary>
    public class UI_RecipeEntry : MonoBehaviour
    {
        [Header("필수 참조")]
        [SerializeField] private RectTransform _titleFrame;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private RectTransform _recipeItems;

        [Header("프리팹")]
        [SerializeField] private UI_RecipeItemEntry _itemPrefab;

        [Header("선택 참조")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _titleIcon;
        [SerializeField] private GameObject _completeMark;
        [SerializeField] private GameObject _currentIndicator;

        [Header("상태 색상")]
        [SerializeField] private Color _normalTitleColor = Color.white;
        [SerializeField] private Color _currentTitleColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color _completeTitleColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color _normalBgColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color _currentBgColor = new Color(1f, 0.95f, 0.7f, 0.95f);
        [SerializeField] private Color _completeBgColor = new Color(0.9f, 0.9f, 0.9f, 0.6f);

        private readonly List<UI_RecipeItemEntry> _itemEntries = new List<UI_RecipeItemEntry>();
        private RecipeData _recipeData;
        private MaterialIconTable _iconTable;
        private RecipeState _state;
        private bool _isCurrent;

        public enum RecipeState
        {
            Pending,
            Current,
            Complete
        }

        public RecipeData RecipeData => _recipeData;
        public RecipeState State => _state;
        public bool IsCurrent => _isCurrent;
        public IReadOnlyList<UI_RecipeItemEntry> ItemEntries => _itemEntries;

        public void SetData(RecipeData recipeData, MaterialIconTable iconTable, bool isCurrent)
        {
            _recipeData = recipeData;
            _iconTable = iconTable;
            _isCurrent = isCurrent;

            string displayName = string.IsNullOrWhiteSpace(recipeData.DisplayName)
                ? recipeData.RecipeId
                : recipeData.DisplayName;

            if (_titleText != null)
            {
                _titleText.text = displayName;
            }

            RebuildItems();
            SetState(isCurrent ? RecipeState.Current : RecipeState.Pending);
        }

        public void SetCurrent(bool isCurrent)
        {
            _isCurrent = isCurrent;

            if (_state != RecipeState.Complete)
            {
                SetState(isCurrent ? RecipeState.Current : RecipeState.Pending);
            }

            if (_currentIndicator != null)
            {
                _currentIndicator.SetActive(isCurrent);
            }
        }

        public void SetState(RecipeState state)
        {
            _state = state;
            ApplyState();
        }

        private void ApplyState()
        {
            Color titleColor;
            Color bgColor;

            switch (_state)
            {
                case RecipeState.Current:
                    titleColor = _currentTitleColor;
                    bgColor = _currentBgColor;
                    break;
                case RecipeState.Complete:
                    titleColor = _completeTitleColor;
                    bgColor = _completeBgColor;
                    break;
                default:
                    titleColor = _normalTitleColor;
                    bgColor = _normalBgColor;
                    break;
            }

            if (_titleText != null)
            {
                _titleText.color = titleColor;
                _titleText.fontStyle = _state == RecipeState.Current ? FontStyles.Bold : FontStyles.Normal;
            }

            if (_background != null)
            {
                _background.color = bgColor;
            }

            if (_completeMark != null)
            {
                _completeMark.SetActive(_state == RecipeState.Complete);
            }

            if (_currentIndicator != null)
            {
                _currentIndicator.SetActive(_isCurrent && _state != RecipeState.Complete);
            }
        }

        private void RebuildItems()
        {
            ClearItems();

            if (_recipeData == null || _iconTable == null || _recipeItems == null || _itemPrefab == null)
            {
                return;
            }

            List<CraftedMaterialType> materials = _recipeData.GetNormalizedRequiredMaterials();
            Dictionary<CraftedMaterialType, int> materialCounts = CountMaterials(materials);

            foreach (var kvp in materialCounts)
            {
                CraftedMaterialType materialType = kvp.Key;
                int count = kvp.Value;

                UI_RecipeItemEntry item = Instantiate(_itemPrefab, _recipeItems);
                item.gameObject.name = $"Item_{materialType}";

                Sprite icon = _iconTable.GetMaterialIcon(materialType);
                item.SetData(materialType, icon, count);

                _itemEntries.Add(item);
            }
        }

        private Dictionary<CraftedMaterialType, int> CountMaterials(List<CraftedMaterialType> materials)
        {
            Dictionary<CraftedMaterialType, int> counts = new Dictionary<CraftedMaterialType, int>();

            foreach (CraftedMaterialType material in materials)
            {
                if (counts.ContainsKey(material))
                {
                    counts[material]++;
                }
                else
                {
                    counts[material] = 1;
                }
            }

            return counts;
        }

        public void UpdateMaterialProgress(Dictionary<CraftedMaterialType, int> ownedMaterials)
        {
            if (ownedMaterials == null)
            {
                return;
            }

            bool allComplete = true;

            foreach (UI_RecipeItemEntry item in _itemEntries)
            {
                int owned = 0;
                if (ownedMaterials.ContainsKey(item.MaterialType))
                {
                    owned = ownedMaterials[item.MaterialType];
                }

                item.SetOwnedCount(owned);

                if (item.State != UI_RecipeItemEntry.MaterialState.Complete)
                {
                    allComplete = false;
                }
            }

            if (allComplete && _state != RecipeState.Complete)
            {
                SetState(RecipeState.Complete);
            }
        }

        public void MarkComplete()
        {
            SetState(RecipeState.Complete);

            foreach (UI_RecipeItemEntry item in _itemEntries)
            {
                item.SetState(UI_RecipeItemEntry.MaterialState.Complete);
            }
        }

        private void ClearItems()
        {
            foreach (UI_RecipeItemEntry item in _itemEntries)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _itemEntries.Clear();
        }

        private void OnDestroy()
        {
            ClearItems();
        }
    }
}
