using UnityEngine;
using UnityEngine.UI;
using System;

public class UI_CustomizingItem : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private GameObject _selectedIndicator;
    [SerializeField] private Button _button;

    [Header("색")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = new Color(0.8f, 0.9f, 1f);

    private Action _onClick;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    public void Setup(CustomizingItemViewData viewData, Action onClickCallback)
    {
        _onClick = onClickCallback;

        if (_iconImage != null && viewData.Icon != null)
        {
            _iconImage.sprite = viewData.Icon;
            _iconImage.color = viewData.IsLocked ? Color.gray : _normalColor;
        }

        SetSelected(viewData.IsSelected);

        if (_button != null)
            _button.interactable = !viewData.IsLocked;
    }

    public void Setup(CustomizingItemSO item, Action onClickCallback)
    {
        _onClick = onClickCallback;

        if (_iconImage != null && item.PreviewIcon != null)
        {
            _iconImage.sprite = item.PreviewIcon;
            _iconImage.color = _normalColor;
        }
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (_selectedIndicator != null)
            _selectedIndicator.SetActive(selected);

        if (_backgroundImage != null)
            _backgroundImage.color = selected ? _selectedColor : _normalColor;
    }

    private void HandleClick()
    {
        _onClick?.Invoke();
    }
}
