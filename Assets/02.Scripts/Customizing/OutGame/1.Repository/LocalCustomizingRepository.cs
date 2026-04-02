using Cysharp.Threading.Tasks;
using UnityEngine;

public class LocalCustomizingRepository : ICustomizingRepository
{
    private readonly string _key;

    public LocalCustomizingRepository(string userId)
    {
        _key = $"{userId}_customizing";
    }

    public UniTask Save(CustomizingSaveData data)
    {
        var serializable = new SerializableSaveData(data);
        string json = JsonUtility.ToJson(serializable, true);
        PlayerPrefs.SetString(_key, json);
        PlayerPrefs.Save();
        return UniTask.CompletedTask;
    }

    public UniTask<CustomizingSaveData> Load()
    {
        if (!PlayerPrefs.HasKey(_key))
            return UniTask.FromResult(CustomizingSaveData.Default);

        string json = PlayerPrefs.GetString(_key);
        var serializable = JsonUtility.FromJson<SerializableSaveData>(json);
        var data = serializable.ToSaveData();
        return UniTask.FromResult(data);
    }

    [System.Serializable]
    private class SerializableSaveData
    {
        public int[] types;
        public string[] itemIds;
        public string[] unlockedItemIds;
        public SerializableSlotData[] slots;
        public string lastSavedAt;

        public SerializableSaveData() { }

        public SerializableSaveData(CustomizingSaveData data)
        {
            lastSavedAt = data.LastSavedAt;

            int count = data.SelectedItems.Count;
            types = new int[count];
            itemIds = new string[count];

            int i = 0;
            foreach (var kvp in data.SelectedItems)
            {
                types[i] = kvp.Key;
                itemIds[i] = kvp.Value;
                i++;
            }

            unlockedItemIds = new string[data.UnlockedItems.Count];
            int j = 0;
            foreach (var id in data.UnlockedItems)
            {
                unlockedItemIds[j++] = id;
            }

            slots = new SerializableSlotData[data.Slots.Count];
            for (int k = 0; k < data.Slots.Count; k++)
            {
                slots[k] = new SerializableSlotData(data.Slots[k]);
            }
        }

        public CustomizingSaveData ToSaveData()
        {
            var data = new CustomizingSaveData();
            data.LastSavedAt = lastSavedAt;

            if (types != null && itemIds != null)
            {
                int count = Mathf.Min(types.Length, itemIds.Length);
                for (int i = 0; i < count; i++)
                {
                    data.SelectedItems[types[i]] = itemIds[i];
                }
            }

            if (unlockedItemIds != null)
            {
                foreach (var id in unlockedItemIds)
                {
                    if (!string.IsNullOrEmpty(id))
                        data.UnlockedItems.Add(id);
                }
            }

            if (slots != null)
            {
                foreach (var slot in slots)
                {
                    data.Slots.Add(slot.ToSlotData());
                }
            }
            data.EnsureSlots();

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
