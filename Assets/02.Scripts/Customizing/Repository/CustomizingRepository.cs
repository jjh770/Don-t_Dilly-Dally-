using UnityEngine;
using System.IO;

/// <summary>
/// 커스터마이징 데이터 저장/로드 담당
/// 초기 구현은 로컬 JSON 파일 저장 기준
/// </summary>
public class CustomizingRepository
{
    private const string SAVE_FILE_NAME = "customizing_save.json";
    private const string PLAYER_PREFS_KEY = "CustomizingSaveData";

    private readonly bool usePlayerPrefs;
    private readonly string savePath;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="usePlayerPrefs">true면 PlayerPrefs 사용, false면 파일 저장</param>
    public CustomizingRepository(bool usePlayerPrefs = false)
    {
        this.usePlayerPrefs = usePlayerPrefs;
        this.savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    }

    /// <summary>
    /// 저장 데이터 존재 여부 확인
    /// </summary>
    public bool HasSaveData()
    {
        if (usePlayerPrefs)
        {
            return PlayerPrefs.HasKey(PLAYER_PREFS_KEY);
        }
        else
        {
            return File.Exists(savePath);
        }
    }

    /// <summary>
    /// DTO 저장
    /// </summary>
    public bool Save(CustomizingDomainDTO dto)
    {
        if (dto == null)
        {
            Debug.LogError("[Repository] Cannot save null DTO");
            return false;
        }

        try
        {
            string json = JsonUtility.ToJson(new SerializableDTO(dto), true);

            if (usePlayerPrefs)
            {
                PlayerPrefs.SetString(PLAYER_PREFS_KEY, json);
                PlayerPrefs.Save();
            }
            else
            {
                File.WriteAllText(savePath, json);
            }

            Debug.Log($"[Repository] Saved customizing data");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Repository] Save failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 저장된 DTO 로드
    /// </summary>
    public CustomizingDomainDTO Load()
    {
        if (!HasSaveData())
        {
            Debug.Log("[Repository] No save data found");
            return null;
        }

        try
        {
            string json;

            if (usePlayerPrefs)
            {
                json = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
            }
            else
            {
                json = File.ReadAllText(savePath);
            }

            var serializable = JsonUtility.FromJson<SerializableDTO>(json);
            var dto = serializable.ToDTO();

            Debug.Log($"[Repository] Loaded customizing data");
            return dto;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Repository] Load failed: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 저장 데이터 삭제
    /// </summary>
    public void Delete()
    {
        if (usePlayerPrefs)
        {
            PlayerPrefs.DeleteKey(PLAYER_PREFS_KEY);
            PlayerPrefs.Save();
        }
        else if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        Debug.Log("[Repository] Deleted customizing save data");
    }

    /// <summary>
    /// JsonUtility용 직렬화 가능한 래퍼
    /// Dictionary는 직접 직렬화가 안 되므로 배열로 변환
    /// </summary>
    [System.Serializable]
    private class SerializableDTO
    {
        public int[] types;
        public string[] itemIds;
        public long savedTimestamp;

        public SerializableDTO() { }

        public SerializableDTO(CustomizingDomainDTO dto)
        {
            savedTimestamp = dto.savedTimestamp;

            int count = dto.selectedItems.Count;
            types = new int[count];
            itemIds = new string[count];

            int i = 0;
            foreach (var kvp in dto.selectedItems)
            {
                types[i] = kvp.Key;
                itemIds[i] = kvp.Value;
                i++;
            }
        }

        public CustomizingDomainDTO ToDTO()
        {
            var dto = new CustomizingDomainDTO();
            dto.savedTimestamp = savedTimestamp;

            if (types != null && itemIds != null)
            {
                int count = Mathf.Min(types.Length, itemIds.Length);
                for (int i = 0; i < count; i++)
                {
                    dto.selectedItems[types[i]] = itemIds[i];
                }
            }

            return dto;
        }
    }
}
