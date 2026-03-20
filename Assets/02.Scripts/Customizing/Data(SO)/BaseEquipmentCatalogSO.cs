using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BaseEquipmentCatalog", menuName = "Customizing/BaseEquipment Catalog")]
public class BaseEquipmentCatalogSO : ScriptableObject
{
    [Header("기본 장착 아이템 (각 종류별 1개)")]
    [Tooltip("상의 (의사 가운)")]
    [SerializeField] private BaseEquipmentItemSO _defaultOutfit;

    [Tooltip("장갑")]
    [SerializeField] private BaseEquipmentItemSO _defaultGloves;

    [Tooltip("하의")]
    [SerializeField] private BaseEquipmentItemSO _defaultPants;

    private Dictionary<BaseEquipmentType, BaseEquipmentItemSO> _itemCache;

    public BaseEquipmentItemSO DefaultOutfit => _defaultOutfit;
    public BaseEquipmentItemSO DefaultGloves => _defaultGloves;
    public BaseEquipmentItemSO DefaultPants => _defaultPants;

    public void Initialize()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _itemCache = new Dictionary<BaseEquipmentType, BaseEquipmentItemSO>
        {
            { BaseEquipmentType.Outfit, _defaultOutfit },
            { BaseEquipmentType.Gloves, _defaultGloves },
            { BaseEquipmentType.Pants, _defaultPants }
        };
    }

    public BaseEquipmentItemSO GetItem(BaseEquipmentType type)
    {
        if (_itemCache == null) BuildCache();

        _itemCache.TryGetValue(type, out var item);
        return item;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _itemCache = null;
    }
#endif
}
