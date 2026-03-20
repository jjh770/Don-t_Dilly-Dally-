using UnityEngine;

/// <summary>
/// 기본 장착 파츠 데이터
/// 커스터마이징 카테고리와 분리된 고정 장착 아이템
/// </summary>
[CreateAssetMenu(fileName = "BaseEquipmentItem", menuName = "Customizing/BaseEquipment Item")]
public class BaseEquipmentItemSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("아이템 고유 ID")]
    [SerializeField] private string itemId;

    [Tooltip("아이템 표시 이름")]
    [SerializeField] private string displayName;

    [Tooltip("기본 장착 종류")]
    [SerializeField] private BaseEquipmentType equipmentType;

    [Header("Visual")]
    [Tooltip("적용할 프리팹")]
    [SerializeField] private GameObject partPrefab;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public BaseEquipmentType EquipmentType => equipmentType;
    public GameObject PartPrefab => partPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = $"BaseEquipment_{equipmentType}_{name}";
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = name;
        }
    }
#endif
}
