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
            if (!equippedItems.TryGetValue(type, out var itemId))
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

[Serializable]
public class CustomizingSlotSaveData
{
    public List<CustomizingSlotData> Slots;

    public const int MaxSlotCount = 3;

    public CustomizingSlotSaveData()
    {
        Slots = new List<CustomizingSlotData>();
    }

    public static CustomizingSlotSaveData CreateDefault()
    {
        var data = new CustomizingSlotSaveData();
        for (int i = 0; i < MaxSlotCount; i++)
        {
            data.Slots.Add(new CustomizingSlotData($"Slot {i + 1}"));
        }
        return data;
    }

    public CustomizingSlotData GetSlot(int index)
    {
        if (index < 0 || index >= Slots.Count)
            return null;
        return Slots[index];
    }

    public void SetSlot(int index, CustomizingSlotData slot)
    {
        if (index < 0 || index >= MaxSlotCount)
            return;

        while (Slots.Count <= index)
        {
            Slots.Add(new CustomizingSlotData($"Slot {Slots.Count + 1}"));
        }

        Slots[index] = slot;
    }
}
