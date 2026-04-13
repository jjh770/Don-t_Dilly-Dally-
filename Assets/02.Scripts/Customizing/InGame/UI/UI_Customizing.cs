using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class UI_Customizing : UIPopupBase
{
    [Header("카테고리 탭")]
    [SerializeField] private List<CategoryTab> _categoryTabs = new();
    [SerializeField] private RectTransform _tabSelectionIndicator;

    [Header("아이템 리스트")]
    [SerializeField] private UI_CustomizingItem _itemSlotButton;
    [SerializeField] private UI_CustomizingItem _itemSlotLockButton;
    [SerializeField] private Transform _itemListParent;
    [SerializeField] private ScrollRect _scrollRect;

    [Header("버튼")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _closeButton;

    [Header("슬롯")]
    [SerializeField] private UI_CustomizingSlotPanel _slotPanel;

    [Header("탭 색상")]
    [SerializeField] private Color _tabSelectedColor = new Color(0.447f, 0.612f, 0.945f, 1f);
    [SerializeField] private Color _tabNormalColor = Color.white;

    private CustomizingUIViewModel _viewModel;
    private List<UI_CustomizingItem> _itemButtons = new();

    public event Action OnClosed;
    public event Action OnSaved;

    public void Initialize(CustomizingUIViewModel viewModel)
    {
        if (_viewModel != null) return;

        _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
        SubscribeToViewModel();

        _slotPanel?.Initialize(_viewModel);
    }

    private void Start()
    {
        if (_viewModel == null) return;

        SetupButtons();
        SetupCategoryTabs();
        _viewModel.OpenCustomizingUI();                          // 화면 열기 처리
        _viewModel.AutoSelectSlot();                // 현재 상태에 맞는 슬롯 선택
        SelectCategory(_viewModel.CurrentCategory);
        UpdateSaveButtonState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCloseClicked();
        }
    }

    private void OnDestroy()
    {
        ClearItemButtons();
        UnsubscribeFromViewModel();
        _viewModel?.Dispose();
    }

    private void SubscribeToViewModel()
    {
        if (_viewModel == null) return;

        _viewModel.OnStateChanged += HandleStateChanged;
        _viewModel.OnCategoryChanged += HandleCategoryChanged;
        _viewModel.OnItemSelected += HandleItemSelected;
    }

    private void UnsubscribeFromViewModel()
    {
        if (_viewModel == null) return;

        _viewModel.OnStateChanged -= HandleStateChanged;
        _viewModel.OnCategoryChanged -= HandleCategoryChanged;
        _viewModel.OnItemSelected -= HandleItemSelected;
    }

    private void SetupButtons()
    {
        _saveButton?.onClick.AddListener(OnSaveClicked);
        _resetButton?.onClick.AddListener(OnResetClicked);
        _closeButton?.onClick.AddListener(OnCloseClicked);
    }

    private void SetupCategoryTabs()
    {
        foreach (var tab in _categoryTabs)
        {
            if (tab.Button == null) continue;

            CustomizingType type = tab.Type;
            tab.Button.onClick.AddListener(() => SelectCategory(type));
        }
    }

    private void HandleStateChanged()
    {
        RefreshItemList();       // 상태가 바뀌면 아이템 목록 다시 그리고
        UpdateSaveButtonState(); // 저장 버튼도 업데이트하기
    }

    private void UpdateSaveButtonState()
    {
        if (_saveButton == null) return;
        if (_viewModel == null) return;

        _saveButton.interactable = _viewModel.CanSave; // 저장 가능 여부는 ViewModel이 판단
    }

    private void HandleCategoryChanged(CustomizingType type)
    {
        UpdateTabVisuals();

        if (_scrollRect != null)  _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void HandleItemSelected(string itemId)
    {
        UpdateItemSelections();
    }

    private void SelectCategory(CustomizingType type)
    {
        _viewModel?.SelectCategory(type);
    }

    private void OnItemClicked(string itemId)
    {
        _viewModel?.SelectOrToggleItem(itemId);
    }

    private void OnSaveClicked()
    {
        _viewModel?.SaveToSelectedSlot();   // 먼저 슬롯에 현재 상태 저장
        _viewModel?.Save();                 // 전체 저장 (MergeMetaFrom에서 업데이트된 슬롯 반영)
        OnSaved?.Invoke();
    }

    private void OnResetClicked()
    {
        _viewModel?.ResetToSaved();
    }

    private void OnCloseClicked()
    {
        _viewModel?.CloseCustomizingUI();

        if (OnClosed != null)
        {
            OnClosed.Invoke();
        }
        else
        {
            Hide();
        }
    }

    private void RefreshItemList()
    {
        ClearItemButtons();

        if (_viewModel == null) return;

        // 지금 카테고리에서 보여야 하는 아이템 목록을 가져와서
        // 버튼을 하나씩 새로 생성
        foreach (var viewData in _viewModel.VisibleItems)
        {
            var button = CreateItemButton(viewData);
            if (button != null)
            {
                _itemButtons.Add(button);
            }
        }
    }

    private UI_CustomizingItem CreateItemButton(CustomizingItemViewData viewData)
    {
        if (_itemListParent == null) return null;

        // 잠금 상태 O -> 잠금용 프리팹
        // 잠금 상태 X -> 일반 프리팹
        var prefab = viewData.IsLocked ? _itemSlotLockButton : _itemSlotButton;
        if (prefab == null) return null;

        var buttonObj = Instantiate(prefab.gameObject, _itemListParent);
        var button = buttonObj.GetComponent<UI_CustomizingItem>();
        button.Setup(viewData, () => OnItemClicked(viewData.ItemId));

        return button;
    }

    private void ClearItemButtons()
    {
        foreach (var button in _itemButtons)
        {
            if (button != null) Destroy(button.gameObject);
        }
        _itemButtons.Clear();
    }

    private void UpdateItemSelections()
    {
        if (_viewModel == null) return;

        var visibleItems = _viewModel.VisibleItems;

        for (int i = 0; i < _itemButtons.Count && i < visibleItems.Count; i++)
        {
            if (_itemButtons[i] != null)
            {
                _itemButtons[i].SetSelected(visibleItems[i].IsSelected);
            }
        }
    }

    private void UpdateTabVisuals()
    {
        if (_viewModel == null) return;

        foreach (var tab in _categoryTabs)
        {
            bool isSelected = tab.Type == _viewModel.CurrentCategory;

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


    protected override void OnShow()
    {
        _viewModel?.OpenCustomizingUI();
    }

    [Serializable]
    public class CategoryTab
    {
        public CustomizingType Type;
        public Button Button;
    }
}
