using System;
using System.Collections.Generic;

public interface ICustomizingManager
{
    event Action OnLoaded;
    event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    event Action OnSaved;
    event Action<string> OnItemUnlocked;

    // 슬롯 이벤트
    event Action OnSlotLoaded;
    event Action<int> OnSlotSelected;
    event Action<int> OnSlotSaved;
    event Action<int, string> OnSlotNameChanged;

    bool IsInitialized { get; }
    bool IsSlotLoaded { get; }
    int SelectedSlotIndex { get; }
    int SlotCount { get; }

    CustomizingItemSO GetEquipped(CustomizingType type);
    CustomizingItemSO GetItemById(string itemId);
    List<CustomizingItemSO> GetAllItemsByType(CustomizingType type);
    List<CustomizingItemSO> GetUnlockedItemsByType(CustomizingType type);
    Dictionary<CustomizingType, string> GetEquippedItemIds();
    IEnumerable<(BaseEquipmentType type, BaseEquipmentItemSO item)> GetAllBaseEquipmentItems();

    bool IsItemLocked(string itemId);
    void UnlockItem(string itemId);
    bool HasLockedEquippedItems();
    bool HasUnsavedChanges();

    EEquipResult ToggleItem(CustomizingItemSO item);
    void Save();
    void OpenCustomizingUI();
    void CloseCustomizingUI();
    void ResetToSaved();

    // 슬롯 API
    void SelectSlot(int index);
    void SaveToSelectedSlot();
    void SaveToSlot(int index);
    void LoadFromSlot(int index);
    void SetSlotName(int index, string name);
    string GetSlotName(int index);
    CustomizingSlotData GetSlot(int index);
    IReadOnlyList<CustomizingSlotData> GetAllSlots();
    bool IsSlotEmpty(int index);
    int FindMatchingSlot();
    void AutoSelectSlot();
}
