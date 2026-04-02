using System;
using System.Collections.Generic;

[Serializable]
public class CustomizingSaveData : ISaveData
{
    public Dictionary<int, string> SelectedItems = new Dictionary<int, string>();
    public HashSet<string> UnlockedItems = new HashSet<string>();
    public List<CustomizingSlotData> Slots = new List<CustomizingSlotData>();
    public string LastSavedAt { get; set; }

    public const int MaxSlotCount = 3;

    public static CustomizingSaveData Default
    {
        get
        {
            var data = new CustomizingSaveData
            {
                SelectedItems = new Dictionary<int, string>(),
                UnlockedItems = new HashSet<string>(),
                LastSavedAt = null
            };
            for (int i = 0; i < MaxSlotCount; i++)
            {
                data.Slots.Add(new CustomizingSlotData($"Slot {i + 1}"));
            }
            return data;
        }
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

    public bool IsUnlocked(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && UnlockedItems.Contains(itemId);
    }

    public bool TryUnlock(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        return UnlockedItems.Add(itemId);
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

    public void EnsureSlots()
    {
        while (Slots.Count < MaxSlotCount)
        {
            Slots.Add(new CustomizingSlotData($"Slot {Slots.Count + 1}"));
        }
    }
}
