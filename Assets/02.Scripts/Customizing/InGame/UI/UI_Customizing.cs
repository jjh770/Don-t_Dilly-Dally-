using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class UI_Customizing : MonoBehaviour
{
    [Header("카테고리 탭")]
    [SerializeField] private List<CategoryTab> _categoryTabs = new();
    [SerializeField] private RectTransform _tabSelectionIndicator;

    [Header("아이템 리스트")]
    [SerializeField] private UI_CustomizingItem _itemPrefab;
    [SerializeField] private Transform _itemListParent;
    [SerializeField] private ScrollRect _scrollRect;

    [Header("버튼")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _closeButton;

    [Header("정보 표시")]
    [SerializeField] private TextMeshProUGUI _selectedItemNameText;

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
    }

    private void Start()
    {
        if (_viewModel == null)
        {
            Debug.LogError("[UI_Customizing] ViewModel이 주입되지 않았습니다. Initialize()를 먼저 호출하세요.");
            return;
        }

        SetupButtons();
        SetupCategoryTabs();

        _viewModel.Open();
        SelectCategory(_viewModel.CurrentCategory);
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
            if (tab.Button != null)
            {
                CustomizingType type = tab.Type;
                tab.Button.onClick.AddListener(() => SelectCategory(type));
            }
        }
    }

    private void HandleStateChanged()
    {
        RefreshItemList();
    }

    private void HandleCategoryChanged(CustomizingType type)
    {
        UpdateTabVisuals();

        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void HandleItemSelected(string itemId)
    {
        UpdateSelectedItemName();
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
        _viewModel?.Save();
        OnSaved?.Invoke();
    }

    private void OnResetClicked()
    {
        _viewModel?.Reset();
    }

    private void OnCloseClicked()
    {
        _viewModel?.Cancel();

        if (OnClosed != null)
        {
            OnClosed.Invoke();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void RefreshItemList()
    {
        ClearItemButtons();

        if (_viewModel == null) return;

        foreach (var viewData in _viewModel.VisibleItems)
        {
            var button = CreateItemButton(viewData);
            _itemButtons.Add(button);
        }
    }

    private UI_CustomizingItem CreateItemButton(CustomizingItemViewData viewData)
    {
        if (_itemPrefab == null || _itemListParent == null) return null;

        var buttonObj = Instantiate(_itemPrefab.gameObject, _itemListParent);
        var button = buttonObj.GetComponent<UI_CustomizingItem>();

        button.Setup(viewData, () => OnItemClicked(viewData.ItemId));

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

    private void UpdateItemSelections()
    {
        if (_viewModel == null) return;

        var visibleItems = _viewModel.VisibleItems;

        for (int i = 0; i < _itemButtons.Count && i < visibleItems.Count; i++)
        {
            _itemButtons[i].SetSelected(visibleItems[i].IsSelected);
        }
    }

    private void UpdateSelectedItemName()
    {
        if (_selectedItemNameText != null && _viewModel != null)
        {
            _selectedItemNameText.text = _viewModel.SelectedItemName;
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

    public void Show()
    {
        gameObject.SetActive(true);
        _viewModel?.Open();
    }

    public void Refresh()
    {
        _viewModel?.Refresh();
    }

    [Serializable]
    public class CategoryTab
    {
        public CustomizingType Type;
        public Button Button;
    }
}
