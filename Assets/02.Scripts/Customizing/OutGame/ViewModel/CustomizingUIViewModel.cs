using System;
using System.Collections.Generic;

public class CustomizingUIViewModel
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
    public bool CanSave => _manager.HasUnsavedChanges() && _manager.HasLockedEquippedItems() == false;

    public CustomizingUIViewModel(ICustomizingManager manager)
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

    // 저장 시도. 잠금 아이템이 장착되어 있으면 false 반환
    public bool TrySave()
    {
        if (_manager.HasLockedEquippedItems())
        {
            return false;
        }

        _manager.Save();
        return true;
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

        // 모든 아이템 표시 (잠금 아이템 포함)
        var items = _manager.GetAllItemsByType(_currentCategory);
        var equippedItem = _manager.GetEquipped(_currentCategory);

        foreach (var item in items)
        {
            bool isEquipped = equippedItem != null && equippedItem.ItemId == item.ItemId;
            // 실제 잠금 상태 = 기본 잠금 && 미해금
            bool isLocked = _manager.IsItemLocked(item.ItemId);

            var viewData = new CustomizingItemViewData(
                itemId: item.ItemId,
                displayName: item.DisplayName,
                icon: item.PreviewIcon,
                isSelected: isEquipped,
                isEquipped: isEquipped,
                isLocked: isLocked,
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
}
