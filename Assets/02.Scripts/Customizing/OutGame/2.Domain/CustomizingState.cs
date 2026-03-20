using System;
using System.Collections.Generic;

// 커스터마이징 런타임 상태 (순수 C#)
// 현재 장착 상태만 관리
public class CustomizingState
{
    private readonly Dictionary<CustomizingType, string> _equippedItemIds;

    public CustomizingState()
    {
        _equippedItemIds = new Dictionary<CustomizingType, string>();
    }

    // 장착 상태 설정
    public void SetEquipped(CustomizingType category, string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Remove(category);
            return;
        }

        _equippedItemIds[category] = itemId;
    }

    // 장착 해제
    public void Remove(CustomizingType category)
    {
        _equippedItemIds.Remove(category);
    }

    // 현재 장착된 아이템 ID 조회
    public string GetEquippedId(CustomizingType category)
    {
        _equippedItemIds.TryGetValue(category, out var itemId);
        return itemId;
    }

    // 장착 여부 확인
    public bool IsEquipped(CustomizingType category)
    {
        return _equippedItemIds.ContainsKey(category);
    }

    // 특정 아이템이 장착되어 있는지 확인
    public bool IsEquippedItem(CustomizingType category, string itemId)
    {
        return _equippedItemIds.TryGetValue(category, out var equipped) && equipped == itemId;
    }

    // 전체 상태 복사본
    public IReadOnlyDictionary<CustomizingType, string> GetAll()
    {
        return _equippedItemIds;
    }

    // 상태 초기화
    public void Clear()
    {
        _equippedItemIds.Clear();
    }

    // DTO로 변환
    public CustomizingDTO ToDTO()
    {
        var dto = new CustomizingDTO();
        foreach (var kvp in _equippedItemIds)
        {
            dto.SetSelectedItemId(kvp.Key, kvp.Value);
        }
        return dto;
    }

    // DTO에서 복원
    public void RestoreFromDTO(CustomizingDTO dto)
    {
        Clear();
        if (dto == null) return;

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            string itemId = dto.GetSelectedItemId(type);
            if (!string.IsNullOrEmpty(itemId))
            {
                _equippedItemIds[type] = itemId;
            }
        }
    }
}
