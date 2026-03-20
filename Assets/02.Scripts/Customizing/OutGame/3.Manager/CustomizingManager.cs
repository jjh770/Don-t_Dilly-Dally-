using UnityEngine;
using System;
using System.Collections.Generic;

public class CustomizingManager : MonoBehaviour
{
    public static CustomizingManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private CustomizingCatalogSO _catalog;
    [SerializeField] private BaseEquipmentCatalogSO _baseEquipmentCatalog;

    [Header("세팅")]
    [SerializeField] private bool usePlayerPrefs = false;
    [SerializeField] private bool autoLoadOnStart = true;

    private Customizing _domain;
    private CustomizingRepository _repository;

    // 이벤트
    public event Action OnInitialized;
    public event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    public event Action OnSaved;
    public event Action OnLoaded;
    

    public Customizing Domain => _domain;
    public CustomizingCatalogSO Catalog => _catalog;
    public BaseEquipmentCatalogSO BaseEquipmentCatalog => _baseEquipmentCatalog;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Initialize();

        if (autoLoadOnStart)
            Load();
    }

    public void Initialize()
    {
        if (_catalog == null)
        {
            Debug.LogError("[CustomizingManager] 카탈로그가 할당되지 않았습니다.");
            return;
        }

        _catalog.Initialize();
        _baseEquipmentCatalog?.Initialize();

        _repository = new CustomizingRepository(usePlayerPrefs);
        _domain = new Customizing(_catalog);

        OnInitialized?.Invoke();
    }

    public void Load()
    {
        if (_domain == null)
        {
            Debug.LogError("[CustomizingManager] 초기화되지 않음");
            return;
        }

        var dto = _repository.Load();

        if (dto != null)
            _domain.RestoreFromDTO(dto);
        else
            _domain.InitializeWithDefaults();

        Debug.Log("[CustomizingManager] 로드 완료");
        OnLoaded?.Invoke();
    }

    public void Save()
    {
        if (_domain == null)
        {
            Debug.LogError("[CustomizingManager] 초기화되지 않음");
            return;
        }

        var dto = _domain.ToDTO();
        bool success = _repository.Save(dto);

        if (success)
        {
            Debug.Log("[CustomizingManager] 저장 완료");
            OnSaved?.Invoke();
        }
    }

    // 아이템 선택
    public EEquipResult SelectItem(CustomizingItemSO item)
    {
        if (_domain == null) return EEquipResult.InvalidItem;
        if (item == null) return EEquipResult.InvalidItem;

        var result = _domain.TryEquip(item);

        if (result.HasChanged())
        {
            OnItemChanged?.Invoke(item.Category, item);
        }

        return result;
    }

    // 재클릭 시 해제
    public EEquipResult ToggleItem(CustomizingItemSO item)
    {
        if (_domain == null) return EEquipResult.InvalidItem;
        if (item == null) return EEquipResult.InvalidItem;

        var result = _domain.ToggleEquip(item);

        if (result.HasChanged())
        {
            if (result == EEquipResult.Equipped)
                OnItemChanged?.Invoke(item.Category, item);
            else if (result == EEquipResult.Unequipped)
                OnItemChanged?.Invoke(item.Category, null);
        }

        return result;
    }

    // ID 기반 선택
    public EEquipResult SelectItemById(string itemId)
    {
        var item = _catalog.GetItemById(itemId);
        return SelectItem(item);
    }

    public void ResetAll()
    {
        _domain?.ResetToDefaults();
        OnLoaded?.Invoke(); // 전체 상태 변경 알림
    }

    // 현재 장착 아이템
    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        var spec = _domain?.GetEquipped(type);
        return spec as CustomizingItemSO;
    }

    // 장착 여부 확인
    public bool IsEquipped(CustomizingItemSO item)
    {
        return _domain?.IsEquipped(item) ?? false;
    }

    // 카테고리별 아이템 목록
    public List<CustomizingItemSO> GetItemsByType(CustomizingType type)
    {
        return _catalog?.GetItemsByType(type) ?? new List<CustomizingItemSO>();
    }

    // 카테고리별 해금 아이템 목록
    public List<CustomizingItemSO> GetUnlockedItemsByType(CustomizingType type)
    {
        return _catalog?.GetUnlockedItemsByType(type) ?? new List<CustomizingItemSO>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
