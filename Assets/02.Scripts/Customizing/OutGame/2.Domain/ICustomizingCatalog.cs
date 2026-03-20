using System.Collections.Generic;

public interface ICustomizingCatalog
{
    ICustomizingItemSpec GetItemById(string itemId);
    ICustomizingItemSpec GetDefaultItem(CustomizingType category);
    IReadOnlyList<ICustomizingItemSpec> GetItemsByCategory(CustomizingType category);
    IReadOnlyList<ICustomizingItemSpec> GetUnlockedItemsByCategory(CustomizingType category);
}
