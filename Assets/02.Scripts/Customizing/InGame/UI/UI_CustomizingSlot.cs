using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CustomizingSlot : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TextMeshProUGUI _nameText;

    [Header("색상")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = new Color(0.447f, 0.612f, 0.945f, 1f);

    private Image _buttonImage;
    private int _slotIndex;

    public event Action<int> OnClicked;
    public event Action<int, string> OnNameChanged;

    public int SlotIndex => _slotIndex;

    public void SetIndex(int index)
    {
        _slotIndex = index;
    }

    private void Awake()
    {
        if (_button != null)
        {
            _buttonImage = _button.GetComponent<Image>();
            _button.onClick.AddListener(() => OnClicked?.Invoke(_slotIndex));
        }

        if (_nameInput != null)
        {
            _nameInput.onEndEdit.AddListener(OnNameEditEnd);
        }
    }

    public void Setup(int index, string name, bool isSelected)
    {
        _slotIndex = index;

        SetName(name);
        SetSelected(isSelected);
    }

    public void SetName(string name)
    {
        if (_nameText != null)
            _nameText.text = name;

        if (_nameInput != null)
            _nameInput.text = name;
    }

    public void SetSelected(bool selected)
    {
        if (_buttonImage != null)
            _buttonImage.color = selected ? _selectedColor : _normalColor;
    }

    private void OnNameEditEnd(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            newName = $"Slot {_slotIndex + 1}";
            SetName(newName);
        }

        OnNameChanged?.Invoke(_slotIndex, newName);
    }
}
