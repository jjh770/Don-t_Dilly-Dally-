using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class UI_Customizing : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CustomizingManager _manager;

    [Header("카테고리 탭")]
    [Tooltip("종류별 탭 버튼들")]
    [SerializeField] private List<CategoryTab> _categoryTabs = new List<CategoryTab>();

    [Tooltip("탭 선택 표시 오브젝트")]
    [SerializeField] private RectTransform _tabSelectionIndicator;

    [Header("아이템 리스트")]
    [Tooltip("아이템 버튼 프리팹")]
    [SerializeField] private UI_CustomizingItem _itemPrefab;

    [Tooltip("아이템 목록이 생성될 부모")]
    [SerializeField] private Transform _itemListParent;

    [Tooltip("스크롤 뷰")]
    [SerializeField] private ScrollRect _scrollRect;

    [Header("버튼")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _closeButton;

    [Header("정보 표시")]
    [Tooltip("선택된 아이템 이름 표시")]
    [SerializeField] private TextMeshProUGUI _selectedItemNameText;

    [Header("탭 색상")]
    [SerializeField] private Color _tabSelectedColor = new Color(0.447f, 0.612f, 0.945f, 1f);
    [SerializeField] private Color _tabNormalColor = Color.white;

    private CustomizingType _currentCategory = CustomizingType.SkinColor;           // 현재 선택된 카테고리
    private List<UI_CustomizingItem> _itemButtons = new List<UI_CustomizingItem>(); // 생성된 아이템 버튼들
    private List<CustomizingItemSO> _currentItems = new List<CustomizingItemSO>();  // 현재 카테고리의 아이템 목록

    private void Start()
    {
        SetupButtons();
        SetupCategoryTabs();

        if (_manager != null)
        {
            _manager.OnItemChanged += HandleItemChanged;
            _manager.OnLoaded += RefreshUI;
        }

        SelectCategory(_currentCategory);
    }

    private void Update()
    {
        // Back 버튼 (Escape 키 / Android 뒤로가기) 처리
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCloseClicked();
        }
    }

    private void OnDestroy()
    {
        if (_manager != null)
        {
            _manager.OnItemChanged -= HandleItemChanged;
            _manager.OnLoaded -= RefreshUI;
        }
    }

    private void SetupButtons()
    {
        if (_saveButton != null)
            _saveButton.onClick.AddListener(OnSaveClicked);

        if (_resetButton != null)
            _resetButton.onClick.AddListener(OnResetClicked);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void SetupCategoryTabs()
    {
        foreach (var tab in _categoryTabs)
        {
            if (tab.Button != null)
            {
                CustomizingType type = tab.Type;
                tab.Button.onClick.AddListener(() => SelectCategory(type));
            }
        }
    }

    public void SelectCategory(CustomizingType type)
    {
        _currentCategory = type;

        UpdateTabVisuals();
        RefreshItemList();

        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateTabVisuals()
    {
        foreach (var tab in _categoryTabs)
        {
            bool isSelected = tab.Type == _currentCategory;

            if (tab.Button != null)
            {
                var image = tab.Button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = isSelected ? _tabSelectedColor : _tabNormalColor;
                }
            }

            if (isSelected && _tabSelectionIndicator != null && tab.Button != null)
            {
                MoveTabSelectionIndicator(tab.Button.transform);
            }
        }
    }

    private void MoveTabSelectionIndicator(Transform tabButton)
    {
        if (_tabSelectionIndicator == null || tabButton == null) return;

        _tabSelectionIndicator.SetParent(tabButton);
        _tabSelectionIndicator.anchoredPosition = new Vector2(0f, -55f);

        _tabSelectionIndicator.gameObject.SetActive(true);
    }

    private void RefreshItemList()
    {
        ClearItemButtons();

        if (_manager == null) return;

        _currentItems = _manager.GetUnlockedItemsByType(_currentCategory);

        var equippedItem = _manager.GetEquipped(_currentCategory);

        foreach (var item in _currentItems)
        {
            var button = CreateItemButton(item);
            button.SetSelected(item == equippedItem);
            _itemButtons.Add(button);
        }
    }

    private UI_CustomizingItem CreateItemButton(CustomizingItemSO item)
    {
        if (_itemPrefab == null || _itemListParent == null) return null;

        var buttonObj = Instantiate(_itemPrefab.gameObject, _itemListParent);
        var button = buttonObj.GetComponent<UI_CustomizingItem>();
        button.Setup(item, () => OnItemClicked(item));

        return button;
    }

    private void ClearItemButtons()
    {
        foreach (var button in _itemButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        _itemButtons.Clear();
    }

    private void OnItemClicked(CustomizingItemSO item)
    {
        if (_manager == null) return;
        _manager.ToggleItem(item);
    }
    private void OnSaveClicked()
    {
        _manager?.Save();
    }

    private void OnResetClicked()
    {
        _manager?.ResetToSaved();
    }

    private void OnCloseClicked()
    {
        _manager?.CloseCustomizingUI();
        gameObject.SetActive(false);
    }

    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item)
    {
        UpdateItemSelections();

        if (_selectedItemNameText != null && item != null)
        {
            _selectedItemNameText.text = item.DisplayName;
        }
    }

    private void UpdateItemSelections()
    {
        if (_manager == null) return;

        var equippedItem = _manager.GetEquipped(_currentCategory);

        for (int i = 0; i < _itemButtons.Count && i < _currentItems.Count; i++)
        {
            _itemButtons[i].SetSelected(_currentItems[i] == equippedItem);
        }
    }

    public void RefreshUI()
    {
        UpdateTabVisuals();
        RefreshItemList();
    }

    [Serializable]
    public class CategoryTab
    {
        public CustomizingType Type;
        public Button Button;
    }
}
