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

    private void OnEnable()
    {
        _selectButton.onClick.AddListener(OnItemClick);
    }

    private void OnItemClick()
    {
        OnSelected?.Invoke(this);
    }

    public void Init(string name, string time)
    {
        _nameText.text = name;
        _dateTimeText.text = time;
    }
}
