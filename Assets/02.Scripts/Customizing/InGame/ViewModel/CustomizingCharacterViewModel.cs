using System;
using System.Collections.Generic;

public class CustomizingCharacterViewModel : IDisposable
{
    private readonly ICustomizingManager _manager;

    public event Action OnLoaded;
    public event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    public event Action OnSaved;

    public bool IsInitialized => _manager != null && _manager.IsInitialized;

    public CustomizingCharacterViewModel(ICustomizingManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        SubscribeToManager();
    }

    public void Dispose()
    {
        UnsubscribeFromManager();
    }

    private void SubscribeToManager()
    {
        _manager.OnLoaded += HandleLoaded;
        _manager.OnItemChanged += HandleItemChanged;
        _manager.OnSaved += HandleSaved;
    }

    private void UnsubscribeFromManager()
    {
        _manager.OnLoaded -= HandleLoaded;
        _manager.OnItemChanged -= HandleItemChanged;
        _manager.OnSaved -= HandleSaved;
    }

    private void HandleLoaded() => OnLoaded?.Invoke();
    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item) => OnItemChanged?.Invoke(type, item);
    private void HandleSaved() => OnSaved?.Invoke();

    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        return _manager.GetEquipped(type);
    }

    public Dictionary<CustomizingType, string> GetEquippedItemIds()
    {
        return _manager.GetEquippedItemIds();
    }

    public IEnumerable<(BaseEquipmentType type, BaseEquipmentItemSO item)> GetAllBaseEquipmentItems()
    {
        return _manager.GetAllBaseEquipmentItems();
    }

    public CustomizingItemSO GetItemById(string itemId)
    {
        return _manager.GetItemById(itemId);
    }
}
