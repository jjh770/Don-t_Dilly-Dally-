using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ICustomizingSlotRepository
{
    UniTask Save(CustomizingSlotSaveData data);
    UniTask<CustomizingSlotSaveData> Load();
}

public class LocalCustomizingSlotRepository : ICustomizingSlotRepository
{
    private readonly string _key;

    public LocalCustomizingSlotRepository(string userId)
    {
        _key = $"{userId}_customizing_slots";
    }

    public UniTask Save(CustomizingSlotSaveData data)
    {
        var serializable = new SerializableSlotSaveData(data);
        string json = JsonUtility.ToJson(serializable, true);
        PlayerPrefs.SetString(_key, json);
        PlayerPrefs.Save();
        return UniTask.CompletedTask;
    }

    public UniTask<CustomizingSlotSaveData> Load()
    {
        if (!PlayerPrefs.HasKey(_key))
            return UniTask.FromResult(CustomizingSlotSaveData.CreateDefault());

        string json = PlayerPrefs.GetString(_key);
        var serializable = JsonUtility.FromJson<SerializableSlotSaveData>(json);
        var data = serializable.ToSaveData();
        return UniTask.FromResult(data);
    }

    [System.Serializable]
    private class SerializableSlotSaveData
    {
        public SerializableSlotData[] slots;

        public SerializableSlotSaveData() { }

        public SerializableSlotSaveData(CustomizingSlotSaveData data)
        {
            slots = new SerializableSlotData[data.Slots.Count];
            for (int i = 0; i < data.Slots.Count; i++)
            {
                slots[i] = new SerializableSlotData(data.Slots[i]);
            }
        }

        public CustomizingSlotSaveData ToSaveData()
        {
            var data = new CustomizingSlotSaveData();
            if (slots != null)
            {
                foreach (var slot in slots)
                {
                    data.Slots.Add(slot.ToSlotData());
                }
            }

            while (data.Slots.Count < CustomizingSlotSaveData.MaxSlotCount)
            {
                data.Slots.Add(new CustomizingSlotData($"Slot {data.Slots.Count + 1}"));
            }

            return data;
        }
    }

    [System.Serializable]
    private class SerializableSlotData
    {
        public string name;
        public int[] types;
        public string[] itemIds;

        public SerializableSlotData() { }

        public SerializableSlotData(CustomizingSlotData slot)
        {
            name = slot.Name;

            int count = slot.EquippedItems.Count;
            types = new int[count];
            itemIds = new string[count];

            int i = 0;
            foreach (var kvp in slot.EquippedItems)
            {
                types[i] = kvp.Key;
                itemIds[i] = kvp.Value;
                i++;
            }
        }

        public CustomizingSlotData ToSlotData()
        {
            var slot = new CustomizingSlotData(name ?? "");

            if (types != null && itemIds != null)
            {
                int count = Mathf.Min(types.Length, itemIds.Length);
                for (int i = 0; i < count; i++)
                {
                    slot.EquippedItems[types[i]] = itemIds[i];
                }
            }

            return slot;
        }
    }
}
