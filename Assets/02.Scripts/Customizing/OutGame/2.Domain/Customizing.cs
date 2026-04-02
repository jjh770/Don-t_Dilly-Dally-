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

    // 장착 가능 여부 검사
    public EEquipResult CanEquip(CustomizingItemSO item)
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
    public EEquipResult TryEquip(CustomizingItemSO item)
    {
        var result = CanEquip(item);
        if (result != EEquipResult.Equipped && result != EEquipResult.AlreadyEquipped)
            return result;

        if (result == EEquipResult.AlreadyEquipped)
            return result;

        _state.SetEquipped(item.Category, item.ItemId);
        return EEquipResult.Equipped;
    }

    // 재클릭 시 해제
    public EEquipResult ToggleEquip(CustomizingItemSO item)
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

    // 현재 장착된 아이템 조회
    public CustomizingItemSO GetEquipped(CustomizingType category)
    {
        var itemId = _state.GetEquippedId(category);
        return !string.IsNullOrEmpty(itemId) ? _catalog.GetItemById(itemId) : null;
    }

    // ========== 상태 조작 메서드 ==========

    // 저장 상태로 복원
    public void RestoreFromState(CustomizingState savedState)
    {
        if (savedState == null) return;
        _state.CopyFrom(savedState);
    }

    // 현재 상태를 외부 상태 객체로 복사
    public void CopyStateTo(CustomizingState target)
    {
        if (target == null) return;
        target.CopyFrom(_state);
    }

    // 슬롯 데이터 적용
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

    // 현재 장착 아이템 ID 스냅샷 반환
    public IReadOnlyDictionary<CustomizingType, string> GetEquippedSnapshot()
    {
        return _state.GetAll();
    }

    // 현재 상태와 슬롯 데이터 일치 여부 확인
    public bool MatchesSlotData(CustomizingSlotData slotData)
    {
        if (slotData == null) return false;
        return slotData.Matches(_state.GetAll());
    }
}
