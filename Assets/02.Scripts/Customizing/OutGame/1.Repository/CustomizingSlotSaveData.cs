using System;
using System.Collections.Generic;

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
