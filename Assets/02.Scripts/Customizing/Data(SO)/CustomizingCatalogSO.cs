using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CustomizingCatalog", menuName = "Customizing/Catalog")]
public class CustomizingCatalogSO : ScriptableObject, ICustomizingCatalog
{
    [Header("SkinColor - 피부색 (Body + Ears)")]
    [SerializeField] private List<CustomizingItemSO> _skinColorItems = new List<CustomizingItemSO>();

    [Header("Hat - 모자")]
    [SerializeField] private List<CustomizingItemSO> _hatItems = new List<CustomizingItemSO>();

    [Header("HairStyle - 머리스타일")]
    [SerializeField] private List<CustomizingItemSO> _hairStyleItems = new List<CustomizingItemSO>();

    [Header("Faces - 표정")]
    [SerializeField] private List<CustomizingItemSO> _facesItems = new List<CustomizingItemSO>();

    [Header("FaceAccessory - 얼굴장식")]
    [SerializeField] private List<CustomizingItemSO> _faceAccessoryItems = new List<CustomizingItemSO>();

    [Header("Glasses - 안경")]
    [SerializeField] private List<CustomizingItemSO> _glassesItems = new List<CustomizingItemSO>();

    [Header("Shoes - 신발")]
    [SerializeField] private List<CustomizingItemSO> _shoesItems = new List<CustomizingItemSO>();

    [Header("Costumes - 코스튬")]
    [SerializeField] private List<CustomizingItemSO> _costumesItems = new List<CustomizingItemSO>();

    // 캐시
    private Dictionary<string, CustomizingItemSO> _itemsById;
    private Dictionary<CustomizingType, CustomizingItemSO> _defaultItems;

    public void Initialize()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _itemsById = new Dictionary<string, CustomizingItemSO>();
        _defaultItems = new Dictionary<CustomizingType, CustomizingItemSO>();

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
                    _itemsById[item.ItemId] = item;
                }

                // 기본 아이템 캐시
                if (item.IsDefault && !_defaultItems.ContainsKey(type))
                {
                    _defaultItems[type] = item;
                }
            }
        }
    }

    private List<CustomizingItemSO> GetListByType(CustomizingType type)
    {
        switch (type)
        {
            case CustomizingType.SkinColor: return _skinColorItems;
            case CustomizingType.Hat: return _hatItems;
            case CustomizingType.HairStyle: return _hairStyleItems;
            case CustomizingType.Faces: return _facesItems;
            case CustomizingType.FaceAccessory: return _faceAccessoryItems;
            case CustomizingType.Glasses: return _glassesItems;
            case CustomizingType.Shoes: return _shoesItems;
            case CustomizingType.Costumes: return _costumesItems;
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
        if (_itemsById == null) BuildCache();

        if (string.IsNullOrEmpty(itemId)) return null;

        _itemsById.TryGetValue(itemId, out var item);
        return item;
    }

    // 특정 종류의 기본 아이템 반환
    public CustomizingItemSO GetDefaultItem(CustomizingType type)
    {
        if (_defaultItems == null) BuildCache();

        _defaultItems.TryGetValue(type, out var item);
        return item;
    }

    // 잠금 해제된 아이템만 반환 (현재는 모든 아이템 반환 - Lock 여부는 CustomizingManager에서 판단)
    public List<CustomizingItemSO> GetUnlockedItemsByType(CustomizingType type)
    {
        return GetItemsByType(type);
    }

    // ICustomizingCatalog 구현
    CustomizingItemSO ICustomizingCatalog.GetItemById(string itemId) => GetItemById(itemId);
    CustomizingItemSO ICustomizingCatalog.GetDefaultItem(CustomizingType category) => GetDefaultItem(category);
    IReadOnlyList<CustomizingItemSO> ICustomizingCatalog.GetItemsByCategory(CustomizingType category) => GetItemsByType(category);
    IReadOnlyList<CustomizingItemSO> ICustomizingCatalog.GetUnlockedItemsByCategory(CustomizingType category) => GetUnlockedItemsByType(category);

#if UNITY_EDITOR
    private void OnValidate()
    {
        _itemsById = null;
        _defaultItems = null;
    }
#endif
}
