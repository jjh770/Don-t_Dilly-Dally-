using System;
using System.Collections.Generic;

[Serializable]
public class CustomizingSlotData
{
    public string Name;
    public Dictionary<int, string> EquippedItems;

    public CustomizingSlotData()
    {
        Name = "";
        EquippedItems = new Dictionary<int, string>();
    }

    public CustomizingSlotData(string name)
    {
        Name = name;
        EquippedItems = new Dictionary<int, string>();
    }

    public void CopyFrom(IReadOnlyDictionary<CustomizingType, string> equippedItems)
    {
        EquippedItems.Clear();
        foreach (var kvp in equippedItems)
        {
            EquippedItems[(int)kvp.Key] = kvp.Value;
        }
    }

    public Dictionary<CustomizingType, string> ToEquippedItems()
    {
        var result = new Dictionary<CustomizingType, string>();
        foreach (var kvp in EquippedItems)
        {
            result[(CustomizingType)kvp.Key] = kvp.Value;
        }
        return result;
    }

    public bool Matches(IReadOnlyDictionary<CustomizingType, string> equippedItems)
    {
        if (EquippedItems.Count != equippedItems.Count)
            return false;

        foreach (var kvp in EquippedItems)
        {
            var type = (CustomizingType)kvp.Key;
            if (equippedItems.TryGetValue(type, out var itemId) == false)
                return false;
            if (itemId != kvp.Value)
                return false;
        }

        return true;
    }

    public bool IsEmpty()
    {
        return EquippedItems == null || EquippedItems.Count == 0;
    }
}
