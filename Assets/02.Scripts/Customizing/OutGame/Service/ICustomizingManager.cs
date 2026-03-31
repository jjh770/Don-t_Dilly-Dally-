using System;
using System.Collections.Generic;

public interface ICustomizingManager
{
    event Action OnLoaded;
    event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    event Action OnSaved;
    event Action<string> OnItemUnlocked;

    bool IsInitialized { get; }

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
}
