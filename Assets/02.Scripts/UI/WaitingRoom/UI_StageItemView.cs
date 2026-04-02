using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_StageItemView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _stageImage;
    [SerializeField] private TMP_Text _stageNumberText;
    [SerializeField] private TMP_Text _stageNameText;
    [SerializeField] private GameObject _lockObject;
    [SerializeField] private Toggle _toggle;

    private int _stageIndex;
    private bool _isAvailable;

    public int StageIndex => _stageIndex;

    public event Action<int> OnSelected;

    public void Initialize(int stageIndex, int stageNumber, string stageName, Sprite stageSprite, bool isAvailable, ToggleGroup toggleGroup)
    {
        _stageIndex = stageIndex;
        _isAvailable = isAvailable;

        if (_stageNumberText != null)
        {
            _stageNumberText.text = stageNumber.ToString();
        }

        if (_stageNameText != null)
        {
            _stageNameText.text = stageName;
        }

        if (_stageImage != null)
        {
            _stageImage.sprite = stageSprite;
            _stageImage.enabled = stageSprite != null;
        }

        if (_lockObject != null)
        {
            _lockObject.SetActive(!isAvailable);
        }

        if (_toggle == null)
        {
            return;
        }

        _toggle.SetIsOnWithoutNotify(false);
        _toggle.group = toggleGroup;
        _toggle.interactable = false;
    }

    public void SetSelectedWithoutNotify(bool isSelected)
    {
        if (_toggle == null)
        {
            return;
        }

        _toggle.SetIsOnWithoutNotify(_isAvailable && isSelected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isAvailable)
        {
            return;
        }

        OnSelected?.Invoke(_stageIndex);
    }
}
