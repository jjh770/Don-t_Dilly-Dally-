using UnityEngine;
using System.IO;

public class CustomizingRepository
{
    private const string SAVE_FILE_NAME = "customizing_save.json";
    private const string PLAYER_PREFS_KEY = "CustomizingSaveData";

    private readonly bool _usePlayerPrefs;
    private readonly string _savePath;

    public CustomizingRepository(bool usePlayerPrefs = false)
    {
        this._usePlayerPrefs = usePlayerPrefs;
        this._savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    }

    public bool HasSaveData()
    {
        if (_usePlayerPrefs)
        {
            return PlayerPrefs.HasKey(PLAYER_PREFS_KEY);
        }
        else
        {
            return File.Exists(_savePath);
        }
    }

    public bool Save(CustomizingDTO dto)
    {
        if (dto == null)
        {
            Debug.LogError("[Repository] null DTO는 저장할 수 없음");
            return false;
        }

        try
        {
            string json = JsonUtility.ToJson(new SerializableDTO(dto), true);

            if (_usePlayerPrefs)
            {
                PlayerPrefs.SetString(PLAYER_PREFS_KEY, json);
                PlayerPrefs.Save();
            }
            else
            {
                File.WriteAllText(_savePath, json);
            }

            Debug.Log($"[Repository] 커스터마이징 데이터 저장 완료");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Repository] 저장 실패: {e.Message}");
            return false;
        }
    }

    public CustomizingDTO Load()
    {
        if (!HasSaveData())
        {
            Debug.Log("[Repository] 저장된 데이터 없음");
            return null;
        }

        try
        {
            string json;

            if (_usePlayerPrefs)
            {
                json = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
            }
            else
            {
                json = File.ReadAllText(_savePath);
            }

            var serializable = JsonUtility.FromJson<SerializableDTO>(json);
            var dto = serializable.ToDTO();

            Debug.Log($"[Repository] 커스터마이징 데이터 로드 완료");
            return dto;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Repository] 로드 실패: {e.Message}");
            return null;
        }
    }

    public void Delete()
    {
        if (_usePlayerPrefs)
        {
            PlayerPrefs.DeleteKey(PLAYER_PREFS_KEY);
            PlayerPrefs.Save();
        }
        else if (File.Exists(_savePath))
        {
            File.Delete(_savePath);
        }

        Debug.Log("[Repository] 커스터마이징 저장 데이터 삭제됨");
    }

    [System.Serializable]
    private class SerializableDTO
    {
        public int[] types;
        public string[] itemIds;
        public long savedTimestamp;

        public SerializableDTO() { }

        public SerializableDTO(CustomizingDTO dto)
        {
            savedTimestamp = dto.SavedTimestamp;

            int count = dto.SelectedItems.Count;
            types = new int[count];
            itemIds = new string[count];

            int i = 0;
            foreach (var kvp in dto.SelectedItems)
            {
                types[i] = kvp.Key;
                itemIds[i] = kvp.Value;
                i++;
            }
        }

        public CustomizingDTO ToDTO()
        {
            var dto = new CustomizingDTO();
            dto.SavedTimestamp = savedTimestamp;

            if (types != null && itemIds != null)
            {
                int count = Mathf.Min(types.Length, itemIds.Length);
                for (int i = 0; i < count; i++)
                {
                    dto.SelectedItems[types[i]] = itemIds[i];
                }
            }

            return dto;
        }
    }
}
