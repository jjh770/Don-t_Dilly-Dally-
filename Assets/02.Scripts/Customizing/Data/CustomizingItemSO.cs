using UnityEngine;

/// <summary>
/// 커스터마이징 아이템의 정적 정보를 담는 ScriptableObject
/// 기획자가 에디터에서 쉽게 수정하고 확장할 수 있도록 설계
///
/// 생성: Assets > Create > Customizing > Item
/// </summary>
[CreateAssetMenu(fileName = "CustomizingItem", menuName = "Customizing/Item")]
public class CustomizingItemSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("아이템 고유 ID (저장/로드에 사용)")]
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

    [Tooltip("잠금 여부 (해금 필요)")]
    [SerializeField] private bool isLocked;

    [Tooltip("정렬 순서 (낮을수록 앞에 표시)")]
    [SerializeField] private int sortOrder;

    // Properties
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
        // ID 자동 생성 (비어있을 경우)
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = $"{customizingType}_{name}";
        }

        // 표시 이름 자동 설정
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = name;
        }
    }
#endif
}
