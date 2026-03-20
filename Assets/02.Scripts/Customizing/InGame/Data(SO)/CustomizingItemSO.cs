using UnityEngine;

[CreateAssetMenu(fileName = "CustomizingItem", menuName = "Customizing/Item")]
public class CustomizingItemSO : ScriptableObject, ICustomizingItemSpec
{
    [Header("Basic Info")]
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private CustomizingType customizingType;

    [Header("Visual")]
    [SerializeField] private Sprite previewIcon;
    [SerializeField] private GameObject partPrefab;

    [Header("Settings")]
    [SerializeField] private bool isDefault;
    [SerializeField] private bool isLocked;
    [SerializeField] private int sortOrder;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public CustomizingType Category => customizingType;
    public bool IsDefault => isDefault;
    public bool IsLocked => isLocked;
    public int SortOrder => sortOrder;
    public Sprite PreviewIcon => previewIcon;
    public GameObject PartPrefab => partPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = $"{customizingType}_{name}";
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = name;
        }
    }
#endif
}
