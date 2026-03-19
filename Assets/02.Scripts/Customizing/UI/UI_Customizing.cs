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

    // 현재 선택된 카테고리
    private CustomizingType _currentCategory = CustomizingType.SkinColor;

    // 생성된 아이템 버튼들
    private List<UI_CustomizingItem> _itemButtons = new List<UI_CustomizingItem>();

    // 현재 카테고리의 아이템 목록
    private List<CustomizingItemSO> _currentItems = new List<CustomizingItemSO>();

    private void Start()
    {
        SetupButtons();
        SetupCategoryTabs();

        if (_manager != null)
        {
            _manager.OnItemChanged += HandleItemChanged;
            _manager.OnLoaded += RefreshUI;
        }

        // 초기 카테고리 표시
        SelectCategory(_currentCategory);
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
            if (tab.button != null)
            {
                CustomizingType type = tab.type;
                tab.button.onClick.AddListener(() => SelectCategory(type));
            }
        }
    }

    public void SelectCategory(CustomizingType type)
    {
        _currentCategory = type;

        // 탭 시각 상태 업데이트
        UpdateTabVisuals();

        // 아이템 목록 갱신
        RefreshItemList();

        // 스크롤 초기화
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateTabVisuals()
    {
        foreach (var tab in _categoryTabs)
        {
            bool isSelected = tab.type == _currentCategory;

            if (tab.button != null)
                tab.button.interactable = !isSelected;

            // 선택된 탭이면 인디케이터 이동
            if (isSelected && _tabSelectionIndicator != null && tab.button != null)
            {
                MoveTabSelectionIndicator(tab.button.transform);
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
        // 기존 버튼 제거
        ClearItemButtons();

        if (_manager == null) return;

        // 현재 카테고리의 아이템 가져오기
        _currentItems = _manager.GetUnlockedItemsByType(_currentCategory);

        // 현재 장착된 아이템
        var equippedItem = _manager.GetEquipped(_currentCategory);

        // 아이템 버튼 생성
        foreach (var item in _currentItems)
        {
            var button = CreateItemButton(item);
            button.SetSelected(item == equippedItem);
            _itemButtons.Add(button);
        }
    }

    private UI_CustomizingItem CreateItemButton(CustomizingItemSO item)
    {
        if (_itemPrefab == null || _itemListParent == null)
        {
            Debug.LogError("[UI_Customizing] 아이템 프리팹 또는 부모가 할당되지 않음");
            return null;
        }

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

        _manager.SelectItem(item);
    }
    private void OnSaveClicked()
    {
        _manager?.Save();
        Debug.Log("[UI_Customizing] 저장 클릭");
    }
    private void OnResetClicked()
    {
        _manager?.ResetAll();
        Debug.Log("[UI_Customizing] 초기화 클릭");
    }

    private void OnCloseClicked()
    {
        // 저장 후 닫기 또는 그냥 닫기
        gameObject.SetActive(false);
    }

    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item)
    {
        // 현재 카테고리와 같으면 선택 상태 갱신
        if (type == _currentCategory)
        {
            UpdateItemSelections();
        }

        // 선택된 아이템 이름 표시
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
        public CustomizingType type;
        public Button button;
    }
}
