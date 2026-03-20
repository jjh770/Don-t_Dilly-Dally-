using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UI_HospitalList : MonoBehaviour
{
    [SerializeField] private UI_HospitalItem _listTemplate;
    [SerializeField] private float _alphaFadeTime;

    private List<UI_HospitalItem> _items = new List<UI_HospitalItem>();
    private CanvasGroup _canvasGroup;
    private bool _isOpened = false;

    public event Action<string> OnDeleteOption;
    public event Action<string> OnSelected;

    private void Start()
    {
        _listTemplate.gameObject.SetActive(false);
        _canvasGroup = GetComponent<CanvasGroup>();

        _canvasGroup.alpha = 0;
        _isOpened = false;
    }

    public void OpenToggle()
    {
        if (_isOpened)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        _canvasGroup.DOKill();
        _canvasGroup.DOFade(1, _alphaFadeTime);
        _isOpened = true;
    }

    public void Hide()
    {
        _canvasGroup.DOKill();
        _canvasGroup.DOFade(0, _alphaFadeTime);
        _isOpened = false;
    }

    public void SetOptions(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            AddItem(name);
        }
    }

    public void OnItemClicked(UI_HospitalItem item)
    {
        OnSelected?.Invoke(item.Name);
    }

    public void AddItem(string name)
    {
        UI_HospitalItem item = (UI_HospitalItem)Instantiate(_listTemplate, this.transform);
        item.gameObject.name = $"Item_{name}";
        item.gameObject.SetActive(true);
        item.OnSelected += OnItemClicked;
        item.Init(name, "date");
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
            Destroy(item.gameObject);
        }

        _items.Clear();
    }

    public void OnDisable()
    {
        RemoveAll();
    }

}
