using System;
using System.Collections.Generic;

[Serializable]
public class CustomizingSaveData : ISaveData
{
    public Dictionary<int, string> SelectedItems = new Dictionary<int, string>();
    public string LastSavedAt { get; set; }

    public static CustomizingSaveData Default => new CustomizingSaveData
    {
        SelectedItems = new Dictionary<int, string>(),
        LastSavedAt = null
    };

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
