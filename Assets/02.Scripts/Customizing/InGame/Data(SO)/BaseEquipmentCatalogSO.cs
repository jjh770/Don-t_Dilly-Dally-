using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 기본 장착 파츠 카탈로그
/// 캐릭터가 항상 착용해야 하는 필수 아이템(Outfit, Gloves, Pants)을 관리
/// </summary>
[CreateAssetMenu(fileName = "BaseEquipmentCatalog", menuName = "Customizing/BaseEquipment Catalog")]
public class BaseEquipmentCatalogSO : ScriptableObject
{
    [Header("기본 장착 아이템 (각 종류별 1개)")]
    [Tooltip("상의 (의사 가운)")]
    [SerializeField] private BaseEquipmentItemSO defaultOutfit;

    [Tooltip("장갑")]
    [SerializeField] private BaseEquipmentItemSO defaultGloves;

    [Tooltip("하의")]
    [SerializeField] private BaseEquipmentItemSO defaultPants;

    // 캐시
    private Dictionary<BaseEquipmentType, BaseEquipmentItemSO> itemCache;

    public BaseEquipmentItemSO DefaultOutfit => defaultOutfit;
    public BaseEquipmentItemSO DefaultGloves => defaultGloves;
    public BaseEquipmentItemSO DefaultPants => defaultPants;

    public void Initialize()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        itemCache = new Dictionary<BaseEquipmentType, BaseEquipmentItemSO>
        {
            { BaseEquipmentType.Outfit, defaultOutfit },
            { BaseEquipmentType.Gloves, defaultGloves },
            { BaseEquipmentType.Pants, defaultPants }
        };
    }

    /// <summary>
    /// 종류별 기본 장착 아이템 가져오기
    /// </summary>
    public BaseEquipmentItemSO GetItem(BaseEquipmentType type)
    {
        if (itemCache == null) BuildCache();

        itemCache.TryGetValue(type, out var item);
        return item;
    }

    /// <summary>
    /// 모든 기본 장착 아이템 가져오기
    /// </summary>
    public IReadOnlyDictionary<BaseEquipmentType, BaseEquipmentItemSO> GetAllItems()
    {
        if (itemCache == null) BuildCache();
        return itemCache;
    }

    /// <summary>
    /// 모든 기본 장착 아이템이 유효한지 검증
    /// </summary>
    public bool IsValid()
    {
        return defaultOutfit != null &&
               defaultGloves != null &&
               defaultPants != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        itemCache = null;
    }
#endif
}
