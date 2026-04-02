using System;
using System.Collections.Generic;

[Serializable]
public class CustomizingSaveData : ISaveData
{
    public Dictionary<int, string> SelectedItems = new Dictionary<int, string>();
    public HashSet<string> UnlockedItems = new HashSet<string>();
    public List<CustomizingSlotData> Slots = new List<CustomizingSlotData>();
    public int SelectedSlotIndex = 0;
    public string LastSavedAt { get; set; }

    public const int MaxSlotCount = 3;
    private const string DefaultSlotNameFormat = "슬롯 {0}";

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
                data.Slots.Add(new CustomizingSlotData(string.Format(DefaultSlotNameFormat, i + 1)));
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
            Slots.Add(new CustomizingSlotData(string.Format(DefaultSlotNameFormat, Slots.Count + 1)));
        }

        Slots[index] = slot;
    }

    public void EnsureSlots()
    {
        while (Slots.Count < MaxSlotCount)
        {
            Slots.Add(new CustomizingSlotData(string.Format(DefaultSlotNameFormat, Slots.Count + 1)));
        }
    }

    public void MergeMetaFrom(CustomizingSaveData source)
    {
        if (source == null)
        {
            EnsureSlots();
            LastSavedAt = DateTime.UtcNow.ToString("o");
            return;
        }

        foreach (var itemId in source.UnlockedItems)
        {
            UnlockedItems.Add(itemId);
        }

        for (int i = 0; i < source.Slots.Count && i < MaxSlotCount; i++)
        {
            SetSlot(i, source.Slots[i]);
        }

        SelectedSlotIndex = source.SelectedSlotIndex;
        EnsureSlots();
        LastSavedAt = DateTime.UtcNow.ToString("o");
    }
}
