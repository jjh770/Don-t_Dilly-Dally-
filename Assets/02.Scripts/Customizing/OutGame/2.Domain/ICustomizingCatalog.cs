using System.Collections.Generic;

public interface ICustomizingCatalog
{
    CustomizingItemSO GetItemById(string itemId);
    CustomizingItemSO GetDefaultItem(CustomizingType category);
    IReadOnlyList<CustomizingItemSO> GetItemsByCategory(CustomizingType category);
    IReadOnlyList<CustomizingItemSO> GetUnlockedItemsByCategory(CustomizingType category);
}
