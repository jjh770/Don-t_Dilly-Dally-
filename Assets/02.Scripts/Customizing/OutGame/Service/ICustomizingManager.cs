using System;
using System.Collections.Generic;

public interface ICustomizingManager
{
    event Action OnLoaded;
    event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    event Action OnSaved;

    bool IsInitialized { get; }

    CustomizingItemSO GetEquipped(CustomizingType type);
    CustomizingItemSO GetItemById(string itemId);
    List<CustomizingItemSO> GetUnlockedItemsByType(CustomizingType type);

    EEquipResult ToggleItem(CustomizingItemSO item);
    void Save();
    void OpenCustomizingUI();
    void CloseCustomizingUI();
    void ResetToSaved();
}
