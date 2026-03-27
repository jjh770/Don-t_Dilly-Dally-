using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_HospitalList : MonoBehaviour
{
    [SerializeField] private UI_HospitalItem _listTemplate;

    private List<UI_HospitalItem> _items = new List<UI_HospitalItem>();

    public event Action<string> OnDeleteOption;
    public event Action<string> OnSelected;

    private void Start()
    {
        _listTemplate.gameObject.SetActive(false);
    }

    public void SetOptions(IEnumerable<MyHospital> hospitals)
    {
        RemoveAll();

        foreach (var hospital in hospitals)
        {
            string dateString = $"마지막 출근 :\n{hospital.Time.ToLocalTime():yy.MM.dd HH:mm}";
            AddItem(hospital.Name, dateString);
        }
    }

    public void OnItemClicked(UI_HospitalItem item)
    {
        OnSelected?.Invoke(item.Name);
    }

    public void OnItemDeleted(UI_HospitalItem item)
    {
        RemoveItem(item);
        OnDeleteOption?.Invoke(item.Name);    
    }

    public void AddItem(string labels, string explain)
    {
        UI_HospitalItem item = (UI_HospitalItem)Instantiate(_listTemplate, this.transform);
        item.gameObject.name = $"Item_{labels}";
        item.gameObject.SetActive(true);
        item.OnSelected += OnItemClicked;
        item.OnDeleted += OnItemDeleted;
        item.Init(labels, explain);
        _items.Add(item);
    }
    public void RemoveItem(UI_HospitalItem item)
    {
        item.OnSelected -= OnItemClicked;
        _items.Remove(item);
        Destroy(item.gameObject);
    }

    private void RemoveAll()
    {
        foreach (UI_HospitalItem item in _items)
        {
            item.OnSelected -= OnItemClicked;
            item.OnDeleted -= OnItemDeleted;
            Destroy(item.gameObject);
        }

        _items.Clear();
    }

    public void OnDisable()
    {
        RemoveAll();
    }

}
