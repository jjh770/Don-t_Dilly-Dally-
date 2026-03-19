using System;
using System.Collections.Generic;

/// <summary>
/// 커스터마이징 상태를 저장/로드하기 위한 순수 데이터 객체
/// 로직을 가지지 않으며, 직렬화 가능한 형태로만 구성
/// </summary>
[Serializable]
public class CustomizingDomainDTO
{
    /// <summary>
    /// 종류별 선택된 아이템 ID
    /// Key: CustomizingType (int로 저장), Value: ItemId
    /// </summary>
    public Dictionary<int, string> selectedItems = new Dictionary<int, string>();

    /// <summary>
    /// 저장 시간
    /// </summary>
    public long savedTimestamp;

    /// <summary>
    /// 기본 생성자
    /// </summary>
    public CustomizingDomainDTO()
    {
        selectedItems = new Dictionary<int, string>();
        savedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// 특정 종류의 선택된 아이템 ID 가져오기
    /// </summary>
    public string GetSelectedItemId(CustomizingType type)
    {
        int key = (int)type;
        if (selectedItems.TryGetValue(key, out string itemId))
        {
            return itemId;
        }
        return null;
    }

    /// <summary>
    /// 특정 종류의 아이템 ID 설정
    /// </summary>
    public void SetSelectedItemId(CustomizingType type, string itemId)
    {
        int key = (int)type;
        selectedItems[key] = itemId;
    }

    /// <summary>
    /// 복사본 생성
    /// </summary>
    public CustomizingDomainDTO Clone()
    {
        var clone = new CustomizingDomainDTO();
        clone.savedTimestamp = savedTimestamp;

        foreach (var kvp in selectedItems)
        {
            clone.selectedItems[kvp.Key] = kvp.Value;
        }

        return clone;
    }
}
