using System;
using System.Collections.Generic;

[Serializable]
public class CustomizingDTO
{
    public Dictionary<int, string> SelectedItems = new Dictionary<int, string>();   // 카테고리 별로 어느 아이템이 선택되었는지
    public long SavedTimestamp;                                                     // 저장 시간

    public CustomizingDTO()
    {
        SelectedItems = new Dictionary<int, string>();
        SavedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public string GetSelectedItemId(CustomizingType type)
    {
        int key = (int)type;
        if (SelectedItems.TryGetValue(key, out string itemId))
        {
            return itemId;
        }
        return null;
    }

    public void SetSelectedItemId(CustomizingType type, string itemId)
    {
        int key = (int)type;
        SelectedItems[key] = itemId;
    }
}
