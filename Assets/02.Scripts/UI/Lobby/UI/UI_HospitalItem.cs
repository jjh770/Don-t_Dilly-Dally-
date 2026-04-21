using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HospitalItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _dateTimeText;
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;

    public string Name => _nameText.text;

    public event Action<UI_HospitalItem> OnSelected;
    public event Action<UI_HospitalItem> OnDeleted;

    private void OnEnable()
    {
        _selectButton.onClick.AddListener(OnItemClick);
        _deleteButton.onClick.AddListener(OnDeleteButtonClick);
    }

    private void OnItemClick()
    {
        SoundManager.Instance.Play(SFXKey.UIButtonClick, SoundType.Local);
        OnSelected?.Invoke(this);
    }

    private void OnDeleteButtonClick()
    {
        SoundManager.Instance.Play(SFXKey.UIButtonClick, SoundType.Local);
        OnDeleted?.Invoke(this);
    }

    public void Init(string label, string explain)
    {
        _nameText.text = label;
        _dateTimeText.text = explain;
    }

    private void OnDisable()
    {
        _selectButton.onClick.RemoveListener(OnItemClick);
        _deleteButton.onClick.RemoveListener(OnDeleteButtonClick);
    }

}
