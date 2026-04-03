using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEditor.Overlays;
using UnityEngine;

public class FirebaseCustomizingRepository : ICustomizingRepository
{
    FirebaseFirestore _db;

    private readonly string _id;

    private readonly string COLLECTION_NAME = "Customizing";

    public FirebaseCustomizingRepository(FirebaseFirestore db, string userId)
    {
        _db = db;
        _id = userId;
    }

    public async UniTask Save(CustomizingSaveData data)
    {
        try
        {
            var dto = new CustomizingSaveDataDTO(data);
            await _db.Collection(COLLECTION_NAME).Document(_id).SetAsync(dto);
            Debug.Log("[FirebaseCustomizingRepository] 저장 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[FirebaseCustomizingRepository] 저장 실패: " + e);
        }    
    }

    public async UniTask<CustomizingSaveData> Load()
    {
        try
        {
            var result = await _db.Collection(COLLECTION_NAME).Document(_id).GetSnapshotAsync();

            CustomizingSaveDataDTO dto = result.ConvertTo<CustomizingSaveDataDTO>();
            Debug.LogFormat("[FirebaseCustomizingRepository] 불러오기 성공");
            if (dto == null)
            {
                Debug.LogWarning("[FirebaseCustomizingRepository] 불러온 데이터가 null 입니다. null을 반환합니다.");
                return null;
            }
            {
                return dto.ToDomain();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[FirebaseCustomizingRepository] 불러오기 실패, null을 반환합니다. :" + e);
            return null;
        }
    }

    [FirestoreData]
    private class CustomizingSaveDataDTO
    {
        [FirestoreProperty]
        public int[] types { get; set; }

        [FirestoreProperty]
        public string[] itemIds { get; set; }

        [FirestoreProperty]
        public string[] unlockedItemIds { get; set; }

        [FirestoreProperty]
        public CustomizingSlotDataDTO[] slots { get; set; }

        [FirestoreProperty]
        public string lastSavedAt { get; set; }

        public CustomizingSaveDataDTO() { }

        public CustomizingSaveDataDTO(CustomizingSaveData data)
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

            slots = new CustomizingSlotDataDTO[data.Slots.Count];
            for (int k = 0; k < data.Slots.Count; k++)
            {
                slots[k] = new CustomizingSlotDataDTO(data.Slots[k]);
            }
        }

        public CustomizingSaveData ToDomain()
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
                    if (string.IsNullOrEmpty(id) == false)
                        data.UnlockedItems.Add(id);
                }
            }

            if (slots != null)
            {
                foreach (var slot in slots)
                {
                    data.Slots.Add(slot.ToDomain());
                }
            }
            data.EnsureSlots();

            return data;
        }
    }

    [FirestoreData]
    private class CustomizingSlotDataDTO
    {
        [FirestoreProperty]
        public string name { get; set; }

        [FirestoreProperty]
        public int[] types { get; set; }

        [FirestoreProperty]
        public string[] itemIds { get; set; }

        public CustomizingSlotDataDTO() { }

        public CustomizingSlotDataDTO(CustomizingSlotData slot)
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

        public CustomizingSlotData ToDomain()
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