using System;
using System.Collections.Generic;

public class Customizing
{
    private readonly ICustomizingCatalog _catalog;
    private readonly CustomizingState _state;

    public CustomizingState State => _state;

    public Customizing(ICustomizingCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
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

    // 장착 가능 여부 검사 (미리보기 착용 허용 - 잠금 체크는 Save 시점에 수행)
    public EEquipResult CanEquip(ICustomizingItemSpec item)
    {
        if (item == null) return EEquipResult.InvalidItem;
        if (_state.IsEquippedItem(item.Category, item.ItemId)) return EEquipResult.AlreadyEquipped;
        return EEquipResult.Equipped;
    }

    // 해제 가능 여부 검사
    public EEquipResult CanUnequip(CustomizingType category)
    {
        if (!_state.IsEquipped(category)) return EEquipResult.AlreadyUnequipped;
        if (!category.CanUnequip()) return EEquipResult.CannotUnequipRequired;
        return EEquipResult.Unequipped;
    }

    // 아이템 장착
    public EEquipResult TryEquip(ICustomizingItemSpec item)
    {
        var result = CanEquip(item);
        if (result != EEquipResult.Equipped && result != EEquipResult.AlreadyEquipped)
            return result;

        if (result == EEquipResult.AlreadyEquipped)
            return result;

        _state.SetEquipped(item.Category, item.ItemId);
        return EEquipResult.Equipped;
    }

    // 재클릭 시 해제 (미리보기 착용 허용 - 잠금 체크는 Save 시점에 수행)
    public EEquipResult ToggleEquip(ICustomizingItemSpec item)
    {
        if (item == null) return EEquipResult.InvalidItem;

        if (_state.IsEquippedItem(item.Category, item.ItemId))
        {
            return TryUnequip(item.Category);
        }

        _state.SetEquipped(item.Category, item.ItemId);
        return EEquipResult.Equipped;
    }

    // 아이템 해제
    public EEquipResult TryUnequip(CustomizingType category)
    {
        var result = CanUnequip(category);
        if (!result.IsSuccess()) return result;
        if (result == EEquipResult.AlreadyUnequipped) return result;

        _state.Remove(category);
        return EEquipResult.Unequipped;
    }

    // 기본값으로 초기화
    public void ResetToDefaults()
    {
        InitializeWithDefaults();
    }

    // 현재 장착된 아이템 조회
    public ICustomizingItemSpec GetEquipped(CustomizingType category)
    {
        var itemId = _state.GetEquippedId(category);
        return !string.IsNullOrEmpty(itemId) ? _catalog.GetItemById(itemId) : null;
    }
}
