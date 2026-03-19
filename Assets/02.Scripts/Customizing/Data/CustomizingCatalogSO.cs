using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CustomizingCatalog", menuName = "Customizing/Catalog")]
public class CustomizingCatalogSO : ScriptableObject
{
    [Header("SkinColor - 피부색 (Body + Ears)")]
    [SerializeField] private List<CustomizingItemSO> skinColorItems = new List<CustomizingItemSO>();

    [Header("Hat - 모자")]
    [SerializeField] private List<CustomizingItemSO> hatItems = new List<CustomizingItemSO>();

    [Header("HairStyle - 머리스타일")]
    [SerializeField] private List<CustomizingItemSO> hairStyleItems = new List<CustomizingItemSO>();

    [Header("Faces - 표정")]
    [SerializeField] private List<CustomizingItemSO> facesItems = new List<CustomizingItemSO>();

    [Header("FaceAccessory - 얼굴장식")]
    [SerializeField] private List<CustomizingItemSO> faceAccessoryItems = new List<CustomizingItemSO>();

    [Header("Glasses - 안경")]
    [SerializeField] private List<CustomizingItemSO> glassesItems = new List<CustomizingItemSO>();

    [Header("Shoes - 신발")]
    [SerializeField] private List<CustomizingItemSO> shoesItems = new List<CustomizingItemSO>();

    [Header("Costumes - 코스튬")]
    [SerializeField] private List<CustomizingItemSO> costumesItems = new List<CustomizingItemSO>();

    // 캐시
    private Dictionary<string, CustomizingItemSO> itemsById;
    private Dictionary<CustomizingType, CustomizingItemSO> defaultItems;

    public void Initialize()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        itemsById = new Dictionary<string, CustomizingItemSO>();
        defaultItems = new Dictionary<CustomizingType, CustomizingItemSO>();

        // 모든 카테고리 순회
        foreach (CustomizingType type in System.Enum.GetValues(typeof(CustomizingType)))
        {
            var items = GetListByType(type);
            foreach (var item in items)
            {
                if (item == null) continue;

                // ID별 캐시
                if (!string.IsNullOrEmpty(item.ItemId))
                {
                    itemsById[item.ItemId] = item;
                }

                // 기본 아이템 캐시
                if (item.IsDefault && !defaultItems.ContainsKey(type))
                {
                    defaultItems[type] = item;
                }
            }
        }
    }

    private List<CustomizingItemSO> GetListByType(CustomizingType type)
    {
        switch (type)
        {
            case CustomizingType.SkinColor: return skinColorItems;
            case CustomizingType.Hat: return hatItems;
            case CustomizingType.HairStyle: return hairStyleItems;
            case CustomizingType.Faces: return facesItems;
            case CustomizingType.FaceAccessory: return faceAccessoryItems;
            case CustomizingType.Glasses: return glassesItems;
            case CustomizingType.Shoes: return shoesItems;
            case CustomizingType.Costumes: return costumesItems;
            default: return new List<CustomizingItemSO>();
        }
    }

    // 특정 종류의 아이템 목록 반환
    public List<CustomizingItemSO> GetItemsByType(CustomizingType type)
    {
        var items = GetListByType(type);
        return items.Where(i => i != null).OrderBy(i => i.SortOrder).ToList();
    }

    // ID로 아이템 찾기
    public CustomizingItemSO GetItemById(string itemId)
    {
        if (itemsById == null) BuildCache();

        if (string.IsNullOrEmpty(itemId)) return null;

        itemsById.TryGetValue(itemId, out var item);
        return item;
    }

    // 특정 종류의 기본 아이템 반환
    public CustomizingItemSO GetDefaultItem(CustomizingType type)
    {
        if (defaultItems == null) BuildCache();

        defaultItems.TryGetValue(type, out var item);
        return item;
    }

    // 잠금 해제된 아이템만 반환
    public List<CustomizingItemSO> GetUnlockedItemsByType(CustomizingType type)
    {
        return GetItemsByType(type).Where(item => !item.IsLocked).ToList();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        itemsById = null;
        defaultItems = null;
    }
#endif
}
