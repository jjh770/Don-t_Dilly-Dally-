using UnityEngine;

[CreateAssetMenu(fileName = "BaseEquipmentItem", menuName = "Customizing/BaseEquipment Item")]
public class BaseEquipmentItemSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("아이템 고유 ID")]
    [SerializeField] private string _itemId;

    [Tooltip("아이템 표시 이름")]
    [SerializeField] private string _displayName;

    [Tooltip("기본 장착 종류")]
    [SerializeField] private BaseEquipmentType _equipmentType;

    [Header("Visual")]
    [Tooltip("적용할 프리팹")]
    [SerializeField] private GameObject _partPrefab;

    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public BaseEquipmentType EquipmentType => _equipmentType;
    public GameObject PartPrefab => _partPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_itemId))
        {
            _itemId = $"BaseEquipment_{_equipmentType}_{name}";
        }

        if (string.IsNullOrEmpty(_displayName))
        {
            _displayName = name;
        }
    }
#endif
}
