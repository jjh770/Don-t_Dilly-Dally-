using System;
using System.Collections.Generic;

public class Customizing
{
    // 현재 선택된 아이템
    private Dictionary<CustomizingType, CustomizingItemSO> _equippedItems;

    // 아이템 카탈로그 참조
    private CustomizingCatalogSO _catalog;

    // 상태 변경 이벤트
    public event Action<CustomizingType, CustomizingItemSO> OnItemEquipped;
    public event Action<CustomizingType> OnItemUnequipped;

    public Customizing(CustomizingCatalogSO catalog)
    {
        this._catalog = catalog;
        this._equippedItems = new Dictionary<CustomizingType, CustomizingItemSO>();
    }

    // 기본 아이템으로 초기화
    public void InitializeWithDefaults()
    {
        _equippedItems.Clear();

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            var defaultItem = _catalog.GetDefaultItem(type);
            if (defaultItem != null)
            {
                _equippedItems[type] = defaultItem;
            }
        }
    }
    public void RestoreFromDTO(CustomizingDTO dto)
    {
        if (dto == null)
        {
            InitializeWithDefaults();
            return;
        }

        _equippedItems.Clear();

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            string itemId = dto.GetSelectedItemId(type);

            if (!string.IsNullOrEmpty(itemId))
            {
                var item = _catalog.GetItemById(itemId);
                if (item != null)
                {
                    _equippedItems[type] = item;
                    continue;
                }
            }

            // 저장된 아이템이 없거나 유효하지 않으면 기본값 사용
            var defaultItem = _catalog.GetDefaultItem(type);
            if (defaultItem != null)
            {
                _equippedItems[type] = defaultItem;
            }
        }
    }

    public CustomizingDTO ToDTO()
    {
        var dto = new CustomizingDTO();

        foreach (var kvp in _equippedItems)
        {
            if (kvp.Value != null)
            {
                dto.SetSelectedItemId(kvp.Key, kvp.Value.ItemId);
            }
        }

        return dto;
    }

    // 아이템 장착
    public bool Equip(CustomizingItemSO item)
    {
        if (item == null)
        {
            UnityEngine.Debug.LogWarning("[Customizing] null 아이템은 장착할 수 없음");
            return false;
        }

        if (item.IsLocked)
        {
            UnityEngine.Debug.LogWarning($"[Customizing] 아이템이 잠김: {item.ItemId}");
            return false;
        }

        CustomizingType type = item.CustomizingType;

        if (_equippedItems.TryGetValue(type, out var current) && current == item)
        {
            return true;
        }

        _equippedItems[type] = item;

        OnItemEquipped?.Invoke(type, item);
        return true;
    }

    // 아이템 해제 (원래 아이템으로)
    public void Unequip(CustomizingType type)
    {
        var defaultItem = _catalog.GetDefaultItem(type);

        if (defaultItem != null)
        {
            _equippedItems[type] = defaultItem;
            OnItemEquipped?.Invoke(type, defaultItem);
        }
        else
        {
            _equippedItems.Remove(type);
            OnItemUnequipped?.Invoke(type);
        }
    }

    // 모든 아이템을 기본값으로 초기화
    public void ResetToDefaults()
    {
        InitializeWithDefaults();

        foreach (var kvp in _equippedItems)
        {
            OnItemEquipped?.Invoke(kvp.Key, kvp.Value);
        }
    }

    // 현재 장착된 아이템 가져오기
    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        _equippedItems.TryGetValue(type, out var item);
        return item;
    }

    // 모든 장착 아이템 가져오기
    public IReadOnlyDictionary<CustomizingType, CustomizingItemSO> GetAllEquipped()
    {
        return _equippedItems;
    }

    // 특정 아이템이 현재 장착되어 있는지 확인
    public bool IsEquipped(CustomizingItemSO item)
    {
        if (item == null) return false;
        return _equippedItems.TryGetValue(item.CustomizingType, out var equipped) && equipped == item;
    }
}
