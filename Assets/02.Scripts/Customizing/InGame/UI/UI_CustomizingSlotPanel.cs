using System.Collections.Generic;
using UnityEngine;

public class UI_CustomizingSlotPanel : MonoBehaviour
{
    [SerializeField] private List<UI_CustomizingSlot> _slots = new();

    private CustomizingUIViewModel _viewModel;
    private int _selectedIndex = 0;

    public void Initialize(CustomizingUIViewModel viewModel)
    {
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            Debug.LogError("[UI_CustomizingSlotPanel] ViewModel이 null입니다.");
            return;
        }

        _viewModel.OnSlotStateChanged += RefreshSlots;
        _viewModel.OnSlotSelected += HandleSlotSelected;

        SetupSlots();

        if (_viewModel.IsSlotLoaded)
        {
            RefreshSlots();
        }
    }

    private void OnDestroy()
    {
        if (_viewModel != null)
        {
            _viewModel.OnSlotStateChanged -= RefreshSlots;
            _viewModel.OnSlotSelected -= HandleSlotSelected;
        }

        foreach (var slot in _slots)
        {
            if (slot == null) continue;
            slot.OnClicked -= OnSlotClicked;
            slot.OnNameChanged -= OnSlotNameChanged;
        }
    }

    private void SetupSlots()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot == null) continue;

            slot.OnClicked += OnSlotClicked;
            slot.OnNameChanged += OnSlotNameChanged;
        }
    }

    private void RefreshSlots()
    {
        if (_viewModel == null) return;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot == null) continue;

            string name = _viewModel.GetSlotName(i);
            bool isSelected = i == _selectedIndex;

            slot.Setup(i, name, isSelected);
        }
    }

    private void OnSlotClicked(int index)
    {
        _viewModel?.SelectSlot(index);
    }

    private void OnSlotNameChanged(int index, string newName)
    {
        _viewModel?.SetSlotName(index, newName);
    }

    private void HandleSlotSelected(int index)
    {
        _selectedIndex = index;

        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i]?.SetSelected(i == index);
        }
    }
}
