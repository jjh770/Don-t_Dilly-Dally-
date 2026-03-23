using UnityEngine;

public class CustomizingItemViewData
{
    public string ItemId { get; }
    public string DisplayName { get; }
    public Sprite Icon { get; }
    public bool IsSelected { get; set; }
    public bool IsEquipped { get; set; }
    public bool IsLocked { get; set; }
    public bool CanUnequip { get; set; }

    public CustomizingItemViewData(
        string itemId,
        string displayName,
        Sprite icon,
        bool isSelected = false,
        bool isEquipped = false,
        bool isLocked = false,
        bool canUnequip = true)
    {
        ItemId = itemId;
        DisplayName = displayName;
        Icon = icon;
        IsSelected = isSelected;
        IsEquipped = isEquipped;
        IsLocked = isLocked;
        CanUnequip = canUnequip;
    }
}
