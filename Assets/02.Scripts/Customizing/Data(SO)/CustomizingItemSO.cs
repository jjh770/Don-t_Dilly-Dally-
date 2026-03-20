using UnityEngine;

[CreateAssetMenu(fileName = "CustomizingItem", menuName = "Customizing/Item")]
public class CustomizingItemSO : ScriptableObject, ICustomizingItemSpec
{
    [Header("Basic Info")]
    [SerializeField] private string _itemId;
    [SerializeField] private string _displayName;
    [SerializeField] private CustomizingType _customizingType;

    [Header("Visual")]
    [SerializeField] private Sprite _previewIcon;
    [SerializeField] private GameObject _partPrefab;

    [Header("Settings")]
    [SerializeField] private bool _isDefault;
    [SerializeField] private bool _isLocked;
    [SerializeField] private int _sortOrder;

    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public CustomizingType Category => _customizingType;
    public bool IsDefault => _isDefault;
    public bool IsLocked => _isLocked;
    public int SortOrder => _sortOrder;
    public Sprite PreviewIcon => _previewIcon;
    public GameObject PartPrefab => _partPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_itemId))
        {
            _itemId = $"{_customizingType}_{name}";
        }

        if (string.IsNullOrEmpty(_displayName))
        {
            _displayName = name;
        }
    }
#endif
}
