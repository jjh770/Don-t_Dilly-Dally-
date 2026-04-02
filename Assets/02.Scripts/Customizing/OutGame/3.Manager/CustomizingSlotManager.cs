using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class CustomizingSlotManager
{
    private readonly Customizing _domain;
    private readonly ICustomizingRepository _repository;
    private readonly Func<CustomizingSaveData> _getSaveData;
    private readonly Action<CustomizingSaveData> _setSaveData;

    private int _selectedSlotIndex = 0;

    public event Action<int> OnSlotSelected;
    public event Action<int> OnSlotSaved;
    public event Action<int, string> OnSlotNameChanged;
    public event Action OnSlotApplied;

    public int SelectedSlotIndex => _selectedSlotIndex;
    public int SlotCount => CustomizingSaveData.MaxSlotCount;

    public CustomizingSlotManager(
        Customizing domain,
        ICustomizingRepository repository,
        Func<CustomizingSaveData> getSaveData,
        Action<CustomizingSaveData> setSaveData)
    {
        _domain = domain;
        _repository = repository;
        _getSaveData = getSaveData;
        _setSaveData = setSaveData;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
            return;

        _selectedSlotIndex = index;
        OnSlotSelected?.Invoke(index);
    }

    public void SaveToSelectedSlot()
    {
        SaveToSlot(_selectedSlotIndex);
    }

    public void SaveToSlot(int index)
    {
        var saveData = _getSaveData();
        if (saveData == null || index < 0 || index >= SlotCount)
            return;

        var slotData = CreateSlotDataFromCurrentState();
        if (slotData == null)
            return;

        var existingSlot = saveData.GetSlot(index);
        slotData.Name = existingSlot?.Name ?? $"Slot {index + 1}";

        saveData.SetSlot(index, slotData);
        _repository.Save(saveData).Forget();

        OnSlotSaved?.Invoke(index);
    }

    public void LoadFromSlot(int index)
    {
        var saveData = _getSaveData();
        if (saveData == null || index < 0 || index >= SlotCount)
            return;

        var slot = saveData.GetSlot(index);
        if (slot == null || slot.IsEmpty())
            return;

        _domain.ApplySlotData(slot);
        OnSlotApplied?.Invoke();
    }

    public void SetSlotName(int index, string name)
    {
        var saveData = _getSaveData();
        if (saveData == null || index < 0 || index >= SlotCount)
            return;

        var slot = saveData.GetSlot(index);
        if (slot == null)
            return;

        slot.Name = name;
        _repository.Save(saveData).Forget();

        OnSlotNameChanged?.Invoke(index, name);
    }

    public string GetSlotName(int index)
    {
        var saveData = _getSaveData();
        if (saveData == null || index < 0 || index >= SlotCount)
            return $"Slot {index + 1}";

        var slot = saveData.GetSlot(index);
        return slot?.Name ?? $"Slot {index + 1}";
    }

    public CustomizingSlotData GetSlot(int index)
    {
        return _getSaveData()?.GetSlot(index);
    }

    public IReadOnlyList<CustomizingSlotData> GetAllSlots()
    {
        return _getSaveData()?.Slots;
    }

    public bool IsSlotEmpty(int index)
    {
        var slot = _getSaveData()?.GetSlot(index);
        return slot == null || slot.IsEmpty();
    }

    public int FindMatchingSlot()
    {
        var saveData = _getSaveData();
        if (saveData == null || _domain == null)
            return -1;

        for (int i = 0; i < saveData.Slots.Count; i++)
        {
            var slot = saveData.Slots[i];
            if (slot != null && !slot.IsEmpty() && _domain.MatchesSlotData(slot))
            {
                return i;
            }
        }

        return -1;
    }

    public void AutoSelectSlot()
    {
        int matchingIndex = FindMatchingSlot();
        SelectSlot(matchingIndex >= 0 ? matchingIndex : 0);
    }

    private CustomizingSlotData CreateSlotDataFromCurrentState()
    {
        if (_domain == null) return null;

        var slotData = new CustomizingSlotData();
        slotData.CopyFrom(_domain.GetEquippedSnapshot());
        return slotData;
    }
}
