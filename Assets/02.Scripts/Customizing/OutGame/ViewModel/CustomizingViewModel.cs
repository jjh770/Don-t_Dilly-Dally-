using System;
using System.Collections.Generic;

public class CustomizingViewModel
{
    private readonly ICustomizingManager _manager;

    private CustomizingType _currentCategory = CustomizingType.SkinColor;
    private List<CustomizingItemViewData> _visibleItems = new();
    private string _selectedItemId;

    public event Action OnStateChanged;
    public event Action<CustomizingType> OnCategoryChanged;
    public event Action<string> OnItemSelected;

    public CustomizingType CurrentCategory => _currentCategory;
    public IReadOnlyList<CustomizingItemViewData> VisibleItems => _visibleItems;
    public string SelectedItemName => GetSelectedItemName();
    public bool HasUnsavedChanges => CheckUnsavedChanges();
    public bool IsInitialized => _manager != null && _manager.IsInitialized;

    public CustomizingViewModel(ICustomizingManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        SubscribeToManager();
    }

    public void Dispose()
    {
        UnsubscribeFromManager();
    }

    private void SubscribeToManager()
    {
        _manager.OnLoaded += HandleLoaded;
        _manager.OnItemChanged += HandleItemChanged;
        _manager.OnSaved += HandleSaved;
    }

    private void UnsubscribeFromManager()
    {
        _manager.OnLoaded -= HandleLoaded;
        _manager.OnItemChanged -= HandleItemChanged;
        _manager.OnSaved -= HandleSaved;
    }

    public void SelectCategory(CustomizingType type)
    {
        if (_currentCategory == type) return;

        _currentCategory = type;
        RefreshVisibleItems();

        OnCategoryChanged?.Invoke(type);
        OnStateChanged?.Invoke();
    }

    public void SelectOrToggleItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        var item = _manager.GetItemById(itemId);
        if (item == null) return;

        var result = _manager.ToggleItem(item);

        if (result.HasChanged())
        {
            _selectedItemId = result == EEquipResult.Equipped ? itemId : null;
            OnItemSelected?.Invoke(_selectedItemId);
        }
    }

    public void Save()
    {
        _manager.Save();
    }

    public void Cancel()
    {
        _manager.CloseCustomizingUI();
    }

    public void Reset()
    {
        _manager.ResetToSaved();
    }

    public void Open()
    {
        _manager.OpenCustomizingUI();
        RefreshVisibleItems();
        OnStateChanged?.Invoke();
    }

    public void Refresh()
    {
        RefreshVisibleItems();
        OnStateChanged?.Invoke();
    }

    private void HandleLoaded()
    {
        RefreshVisibleItems();
        OnStateChanged?.Invoke();
    }

    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item)
    {
        if (type == _currentCategory)
        {
            UpdateItemSelections();
        }

        _selectedItemId = item?.ItemId;
        OnItemSelected?.Invoke(_selectedItemId);
        OnStateChanged?.Invoke();
    }

    private void HandleSaved()
    {
        OnStateChanged?.Invoke();
    }

    private void RefreshVisibleItems()
    {
        _visibleItems.Clear();

        var items = _manager.GetUnlockedItemsByType(_currentCategory);
        var equippedItem = _manager.GetEquipped(_currentCategory);

        foreach (var item in items)
        {
            bool isEquipped = equippedItem != null && equippedItem.ItemId == item.ItemId;

            var viewData = new CustomizingItemViewData(
                itemId: item.ItemId,
                displayName: item.DisplayName,
                icon: item.PreviewIcon,
                isSelected: isEquipped,
                isEquipped: isEquipped,
                isLocked: item.IsLocked,
                canUnequip: true
            );

            _visibleItems.Add(viewData);
        }
    }

    private void UpdateItemSelections()
    {
        var equippedItem = _manager.GetEquipped(_currentCategory);
        string equippedId = equippedItem?.ItemId;

        foreach (var viewData in _visibleItems)
        {
            bool isEquipped = viewData.ItemId == equippedId;
            viewData.IsSelected = isEquipped;
            viewData.IsEquipped = isEquipped;
        }
    }

    private string GetSelectedItemName()
    {
        if (string.IsNullOrEmpty(_selectedItemId)) return "";

        var item = _manager.GetItemById(_selectedItemId);
        return item?.DisplayName ?? "";
    }

    private bool CheckUnsavedChanges()
    {
        return false;
    }
}
