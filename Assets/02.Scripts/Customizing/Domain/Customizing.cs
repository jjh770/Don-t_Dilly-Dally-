using System;
using System.Collections.Generic;

/// <summary>
/// 커스터마이징 도메인
/// 현재 장착 상태와 규칙을 담당하는 핵심 로직
///
/// 핵심 규칙:
/// - 종류별로 아이템은 1개만 선택 가능
/// - 같은 종류의 새 아이템을 선택하면 기존 아이템은 교체
/// - 존재하지 않는 아이템 ID는 허용하지 않음
/// </summary>
public class Customizing
{
    // 현재 선택된 아이템 (종류별 1개씩)
    private Dictionary<CustomizingType, CustomizingItemSO> equippedItems;

    // 아이템 카탈로그 참조
    private CustomizingCatalogSO catalog;

    // 상태 변경 이벤트
    public event Action<CustomizingType, CustomizingItemSO> OnItemEquipped;
    public event Action<CustomizingType> OnItemUnequipped;

    /// <summary>
    /// 생성자
    /// </summary>
    public Customizing(CustomizingCatalogSO catalog)
    {
        this.catalog = catalog;
        this.equippedItems = new Dictionary<CustomizingType, CustomizingItemSO>();
    }

    /// <summary>
    /// 기본 아이템으로 초기화
    /// </summary>
    public void InitializeWithDefaults()
    {
        equippedItems.Clear();

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            var defaultItem = catalog.GetDefaultItem(type);
            if (defaultItem != null)
            {
                equippedItems[type] = defaultItem;
            }
        }
    }

    /// <summary>
    /// DTO로부터 상태 복원
    /// </summary>
    public void RestoreFromDTO(CustomizingDomainDTO dto)
    {
        if (dto == null)
        {
            InitializeWithDefaults();
            return;
        }

        equippedItems.Clear();

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            string itemId = dto.GetSelectedItemId(type);

            if (!string.IsNullOrEmpty(itemId))
            {
                var item = catalog.GetItemById(itemId);
                if (item != null)
                {
                    equippedItems[type] = item;
                    continue;
                }
            }

            // 저장된 아이템이 없거나 유효하지 않으면 기본값 사용
            var defaultItem = catalog.GetDefaultItem(type);
            if (defaultItem != null)
            {
                equippedItems[type] = defaultItem;
            }
        }
    }

    /// <summary>
    /// 현재 상태를 DTO로 변환
    /// </summary>
    public CustomizingDomainDTO ToDTO()
    {
        var dto = new CustomizingDomainDTO();

        foreach (var kvp in equippedItems)
        {
            if (kvp.Value != null)
            {
                dto.SetSelectedItemId(kvp.Key, kvp.Value.ItemId);
            }
        }

        return dto;
    }

    /// <summary>
    /// 아이템 장착
    /// </summary>
    /// <param name="item">장착할 아이템</param>
    /// <returns>장착 성공 여부</returns>
    public bool Equip(CustomizingItemSO item)
    {
        if (item == null)
        {
            UnityEngine.Debug.LogWarning("[Customizing] Cannot equip null item");
            return false;
        }

        if (item.IsLocked)
        {
            UnityEngine.Debug.LogWarning($"[Customizing] Item is locked: {item.ItemId}");
            return false;
        }

        CustomizingType type = item.CustomizingType;

        // 같은 아이템이면 무시
        if (equippedItems.TryGetValue(type, out var current) && current == item)
        {
            return true;
        }

        // 기존 아이템 교체
        equippedItems[type] = item;

        OnItemEquipped?.Invoke(type, item);
        return true;
    }

    /// <summary>
    /// ID로 아이템 장착
    /// </summary>
    public bool EquipById(string itemId)
    {
        var item = catalog.GetItemById(itemId);
        if (item == null)
        {
            UnityEngine.Debug.LogWarning($"[Customizing] Item not found: {itemId}");
            return false;
        }
        return Equip(item);
    }

    /// <summary>
    /// 특정 종류 아이템 해제 (기본값으로 되돌림)
    /// </summary>
    public void Unequip(CustomizingType type)
    {
        var defaultItem = catalog.GetDefaultItem(type);

        if (defaultItem != null)
        {
            equippedItems[type] = defaultItem;
            OnItemEquipped?.Invoke(type, defaultItem);
        }
        else
        {
            equippedItems.Remove(type);
            OnItemUnequipped?.Invoke(type);
        }
    }

    /// <summary>
    /// 모든 아이템을 기본값으로 초기화
    /// </summary>
    public void ResetToDefaults()
    {
        InitializeWithDefaults();

        foreach (var kvp in equippedItems)
        {
            OnItemEquipped?.Invoke(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// 현재 장착된 아이템 가져오기
    /// </summary>
    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        equippedItems.TryGetValue(type, out var item);
        return item;
    }

    /// <summary>
    /// 모든 장착 아이템 가져오기
    /// </summary>
    public IReadOnlyDictionary<CustomizingType, CustomizingItemSO> GetAllEquipped()
    {
        return equippedItems;
    }

    /// <summary>
    /// 특정 종류에 아이템이 장착되어 있는지 확인
    /// </summary>
    public bool IsEquipped(CustomizingType type)
    {
        return equippedItems.ContainsKey(type) && equippedItems[type] != null;
    }

    /// <summary>
    /// 특정 아이템이 현재 장착되어 있는지 확인
    /// </summary>
    public bool IsEquipped(CustomizingItemSO item)
    {
        if (item == null) return false;
        return equippedItems.TryGetValue(item.CustomizingType, out var equipped) && equipped == item;
    }
}
