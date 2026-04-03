using Cysharp.Threading.Tasks;
using Firebase.Firestore;
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

            return dto.ToDomain();
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
        public int[] Types { get; set; }

        [FirestoreProperty]
        public string[] ItemIds { get; set; }

        [FirestoreProperty]
        public string[] UnlockedItemIds { get; set; }

        [FirestoreProperty]
        public CustomizingSlotDataDTO[] Slots { get; set; }

        [FirestoreProperty]
        public string LastSavedAt { get; set; }

        public CustomizingSaveDataDTO() { }

        public CustomizingSaveDataDTO(CustomizingSaveData data)
        {
            LastSavedAt = data.LastSavedAt;

            int count = data.SelectedItems.Count;
            Types = new int[count];
            ItemIds = new string[count];

            int i = 0;
            foreach (var kvp in data.SelectedItems)
            {
                Types[i] = kvp.Key;
                ItemIds[i] = kvp.Value;
                i++;
            }

            UnlockedItemIds = new string[data.UnlockedItems.Count];
            int j = 0;
            foreach (var id in data.UnlockedItems)
            {
                UnlockedItemIds[j++] = id;
            }

            Slots = new CustomizingSlotDataDTO[data.Slots.Count];
            for (int k = 0; k < data.Slots.Count; k++)
            {
                Slots[k] = new CustomizingSlotDataDTO(data.Slots[k]);
            }
        }

        public CustomizingSaveData ToDomain()
        {
            var data = new CustomizingSaveData();
            data.LastSavedAt = LastSavedAt;

            if (Types != null && ItemIds != null)
            {
                int count = Mathf.Min(Types.Length, ItemIds.Length);
                for (int i = 0; i < count; i++)
                {
                    data.SelectedItems[Types[i]] = ItemIds[i];
                }
            }

            if (UnlockedItemIds != null)
            {
                foreach (var id in UnlockedItemIds)
                {
                    if (string.IsNullOrEmpty(id) == false)
                        data.UnlockedItems.Add(id);
                }
            }

            if (Slots != null)
            {
                foreach (var slot in Slots)
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
        public string Name { get; set; }

        [FirestoreProperty]
        public int[] Types { get; set; }

        [FirestoreProperty]
        public string[] ItemIds { get; set; }

        public CustomizingSlotDataDTO() { }

        public CustomizingSlotDataDTO(CustomizingSlotData slot)
        {
            Name = slot.Name;

            int count = slot.EquippedItems.Count;
            Types = new int[count];
            ItemIds = new string[count];

            int i = 0;
            foreach (var kvp in slot.EquippedItems)
            {
                Types[i] = kvp.Key;
                ItemIds[i] = kvp.Value;
                i++;
            }
        }

        public CustomizingSlotData ToDomain()
        {
            var slot = new CustomizingSlotData(Name ?? "");

            if (Types != null && ItemIds != null)
            {
                int count = Mathf.Min(Types.Length, ItemIds.Length);
                for (int i = 0; i < count; i++)
                {
                    slot.EquippedItems[Types[i]] = ItemIds[i];
                }
            }

            return slot;
        }
    }
}