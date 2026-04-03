using System;
using System.Collections.Generic;

public class Customizing
{
    private readonly ICustomizingCatalog _catalog;
    private readonly CustomizingState _state;

    public Customizing(ICustomizingCatalog catalog)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));

        _catalog = catalog;
        _state = new CustomizingState();
    }

    public void InitializeWithDefaults()
    {
        _state.Clear();

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            var defaultItem = _catalog.GetDefaultItem(type);
            if (defaultItem != null)
            {
                _state.SetEquipped(type, defaultItem.ItemId);
            }
        }
    }

    public void RestoreFromSaveData(CustomizingSaveData saveData)
    {
        if (saveData == null)
        {
            InitializeWithDefaults();
            return;
        }

        _state.Clear();

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            string itemId = saveData.GetSelectedItemId(type);
            var item = !string.IsNullOrEmpty(itemId) ? _catalog.GetItemById(itemId) : null;

            if (item != null)
            {
                _state.SetEquipped(type, item.ItemId);
            }
            else if (type.IsRequired())
            {
                var defaultItem = _catalog.GetDefaultItem(type);
                if (defaultItem != null)
                {
                    _state.SetEquipped(type, defaultItem.ItemId);
                }
            }
        }
    }

    public CustomizingSaveData ToSaveData()
    {
        return _state.ToSaveData();
    }

    public EEquipResult CanEquip(CustomizingItemSO item)
    {
        if (item == null) return EEquipResult.InvalidItem;
        if (_state.IsEquippedItem(item.Category, item.ItemId)) return EEquipResult.AlreadyEquipped;
        return EEquipResult.Equipped;
    }

    public EEquipResult CanUnequip(CustomizingType category)
    {
        if (_state.IsEquipped(category) == false) return EEquipResult.AlreadyUnequipped;
        if (category.CanUnequip() == false) return EEquipResult.CannotUnequipRequired;
        return EEquipResult.Unequipped;
    }

    public EEquipResult TryEquip(CustomizingItemSO item)
    {
        var result = CanEquip(item);
        if (result != EEquipResult.Equipped) return result;

        _state.SetEquipped(item.Category, item.ItemId);
        return EEquipResult.Equipped;
    }

    public EEquipResult ToggleEquip(CustomizingItemSO item)
    {
        if (item == null) return EEquipResult.InvalidItem;

        if (_state.IsEquippedItem(item.Category, item.ItemId))
        {
            return TryUnequip(item.Category);
        }

        return TryEquip(item);
    }

    public EEquipResult TryUnequip(CustomizingType category)
    {
        var result = CanUnequip(category);
        if (result.IsSuccess() == false) return result;
        if (result == EEquipResult.AlreadyUnequipped) return result;

        _state.Remove(category);
        return EEquipResult.Unequipped;
    }

    public CustomizingItemSO GetEquipped(CustomizingType category)
    {
        var itemId = _state.GetEquippedId(category);
        return !string.IsNullOrEmpty(itemId) ? _catalog.GetItemById(itemId) : null;
    }

    // ========== 상태 조작 메서드 ==========
    public void RestoreFromState(CustomizingState savedState)
    {
        if (savedState == null) return;
        _state.CopyFrom(savedState);
    }

    public void CopyStateTo(CustomizingState target)
    {
        if (target == null) return;
        target.CopyFrom(_state);
    }

    public void ApplySlotData(CustomizingSlotData slotData)
    {
        if (slotData == null || slotData.IsEmpty()) return;

        _state.Clear();
        var equippedItems = slotData.ToEquippedItems();
        foreach (var kvp in equippedItems)
        {
            _state.SetEquipped(kvp.Key, kvp.Value);
        }
    }

    public IReadOnlyDictionary<CustomizingType, string> GetEquippedSnapshot()
    {
        return _state.GetAll();
    }

    public bool MatchesSlotData(CustomizingSlotData slotData)
    {
        if (slotData == null) return false;
        return slotData.Matches(_state.GetAll());
    }
}
