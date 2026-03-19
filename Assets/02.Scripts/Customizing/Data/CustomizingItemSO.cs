using UnityEngine;

[CreateAssetMenu(fileName = "CustomizingItem", menuName = "Customizing/Item")]
public class CustomizingItemSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("아이템 고유 ID")]
    [SerializeField] private string itemId;

    [Tooltip("아이템 표시 이름")]
    [SerializeField] private string displayName;

    [Tooltip("커스터마이징 종류")]
    [SerializeField] private CustomizingType customizingType;

    [Header("Visual")]
    [Tooltip("UI에 표시할 미리보기 아이콘")]
    [SerializeField] private Sprite previewIcon;

    [Tooltip("적용할 프리팹")]
    [SerializeField] private GameObject partPrefab;

    [Header("Settings")]
    [Tooltip("기본 장착 아이템 여부")]
    [SerializeField] private bool isDefault;

    [Tooltip("잠금 여부")]
    [SerializeField] private bool isLocked;

    [Tooltip("정렬 순서")]
    [SerializeField] private int sortOrder;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public CustomizingType CustomizingType => customizingType;
    public Sprite PreviewIcon => previewIcon;
    public GameObject PartPrefab => partPrefab;
    public bool IsDefault => isDefault;
    public bool IsLocked => isLocked;
    public int SortOrder => sortOrder;

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
