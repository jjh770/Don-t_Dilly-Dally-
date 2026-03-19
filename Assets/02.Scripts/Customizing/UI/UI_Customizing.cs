using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class UI_Customizing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CustomizingManager manager;

    [Header("Category Tabs")]
    [Tooltip("종류별 탭 버튼들")]
    [SerializeField] private List<CategoryTab> categoryTabs = new List<CategoryTab>();

    [Tooltip("탭 선택 표시 오브젝트 (선택된 탭 아래로 이동)")]
    [SerializeField] private RectTransform tabSelectionIndicator;

    [Header("Item List")]
    [Tooltip("아이템 버튼 프리팹")]
    [SerializeField] private UI_CustomizingItem itemPrefab;

    [Tooltip("아이템 목록이 생성될 부모")]
    [SerializeField] private Transform itemListParent;

    [Tooltip("스크롤 뷰 (선택 시 스크롤 초기화용)")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    [Header("Info Display")]
    [Tooltip("선택된 아이템 이름 표시")]
    [SerializeField] private TextMeshProUGUI selectedItemNameText;

    // 현재 선택된 카테고리
    private CustomizingType currentCategory = CustomizingType.SkinColor;

    // 생성된 아이템 버튼들
    private List<UI_CustomizingItem> itemButtons = new List<UI_CustomizingItem>();

    // 현재 카테고리의 아이템 목록
    private List<CustomizingItemSO> currentItems = new List<CustomizingItemSO>();

    private void Start()
    {
        SetupButtons();
        SetupCategoryTabs();

        if (manager != null)
        {
            manager.OnItemChanged += HandleItemChanged;
            manager.OnLoaded += RefreshUI;
        }

        // 초기 카테고리 표시
        SelectCategory(currentCategory);
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.OnItemChanged -= HandleItemChanged;
            manager.OnLoaded -= RefreshUI;
        }
    }

    private void SetupButtons()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void SetupCategoryTabs()
    {
        foreach (var tab in categoryTabs)
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
        currentCategory = type;

        // 탭 시각 상태 업데이트
        UpdateTabVisuals();

        // 아이템 목록 갱신
        RefreshItemList();

        // 스크롤 초기화
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateTabVisuals()
    {
        foreach (var tab in categoryTabs)
        {
            bool isSelected = tab.type == currentCategory;

            if (tab.button != null)
                tab.button.interactable = !isSelected;

            // 선택된 탭이면 인디케이터 이동
            if (isSelected && tabSelectionIndicator != null && tab.button != null)
            {
                MoveTabSelectionIndicator(tab.button.transform);
            }
        }
    }

    private void MoveTabSelectionIndicator(Transform tabButton)
    {
        if (tabSelectionIndicator == null || tabButton == null) return;

        tabSelectionIndicator.position = tabButton.position;

        tabSelectionIndicator.gameObject.SetActive(true);
    }

    private void RefreshItemList()
    {
        // 기존 버튼 제거
        ClearItemButtons();

        if (manager == null) return;

        // 현재 카테고리의 아이템 가져오기
        currentItems = manager.GetUnlockedItemsByType(currentCategory);

        // 현재 장착된 아이템
        var equippedItem = manager.GetEquipped(currentCategory);

        // 아이템 버튼 생성
        foreach (var item in currentItems)
        {
            var button = CreateItemButton(item);
            button.SetSelected(item == equippedItem);
            itemButtons.Add(button);
        }
    }

    private UI_CustomizingItem CreateItemButton(CustomizingItemSO item)
    {
        if (itemPrefab == null || itemListParent == null)
        {
            Debug.LogError("[UI_Customizing] Item prefab or parent not assigned");
            return null;
        }

        var buttonObj = Instantiate(itemPrefab.gameObject, itemListParent);
        var button = buttonObj.GetComponent<UI_CustomizingItem>();

        button.Setup(item, () => OnItemClicked(item));

        return button;
    }

    private void ClearItemButtons()
    {
        foreach (var button in itemButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        itemButtons.Clear();
    }

    private void OnItemClicked(CustomizingItemSO item)
    {
        if (manager == null) return;

        manager.SelectItem(item);
    }
    private void OnSaveClicked()
    {
        manager?.Save();
        Debug.Log("[UI_Customizing] Save clicked");
    }
    private void OnResetClicked()
    {
        manager?.ResetAll();
        Debug.Log("[UI_Customizing] Reset clicked");
    }

    private void OnCloseClicked()
    {
        // 저장 후 닫기 또는 그냥 닫기
        gameObject.SetActive(false);
    }

    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item)
    {
        // 현재 카테고리와 같으면 선택 상태 갱신
        if (type == currentCategory)
        {
            UpdateItemSelections();
        }

        // 선택된 아이템 이름 표시
        if (selectedItemNameText != null && item != null)
        {
            selectedItemNameText.text = item.DisplayName;
        }
    }

    private void UpdateItemSelections()
    {
        if (manager == null) return;

        var equippedItem = manager.GetEquipped(currentCategory);

        for (int i = 0; i < itemButtons.Count && i < currentItems.Count; i++)
        {
            itemButtons[i].SetSelected(currentItems[i] == equippedItem);
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
