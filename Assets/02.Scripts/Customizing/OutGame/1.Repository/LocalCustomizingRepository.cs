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

            return data;
        }
    }
}
