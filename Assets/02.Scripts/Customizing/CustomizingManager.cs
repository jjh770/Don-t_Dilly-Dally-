using UnityEngine;
using System;
using System.Collections.Generic;

public class CustomizingManager : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("아이템 카탈로그 SO")]
    [SerializeField] private CustomizingCatalogSO catalog;

    [Tooltip("커스터마이징 결과를 적용할 플레이어")]
    [SerializeField] private CustomizingPlayer customizingPlayer;

    [Header("세팅")]
    [Tooltip("PlayerPrefs 사용 여부")]
    [SerializeField] private bool usePlayerPrefs = false;

    [Tooltip("시작 시 자동 로드")]
    [SerializeField] private bool autoLoadOnStart = true;

    private Customizing customizing;            // 도메인
    private CustomizingRepository repository;   // 저장소

    // 이벤트
    public event Action OnInitialized;
    public event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    public event Action OnSaved;
    public event Action OnLoaded;

    public static CustomizingManager Instance { get; private set; }

    public Customizing Domain => customizing;
    public CustomizingCatalogSO Catalog => catalog;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
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
        {
            Load();
        }
    }

    public void Initialize()
    {
        if (catalog == null)
        {
            Debug.LogError("[CustomizingManager] 카탈로그가 할당되지 않았습니다.");
            return;
        }

        
        catalog.Initialize();                                       // 카탈로그 초기화
        repository = new CustomizingRepository(usePlayerPrefs);     // 저장소 생성
        customizing = new Customizing(catalog);                     // 도메인 생성

        
        customizing.OnItemEquipped += HandleItemEquipped;
        customizing.OnItemUnequipped += HandleItemUnequipped;
        OnInitialized?.Invoke();
    }

    public void Load()
    {
        if (customizing == null)
        {
            Debug.LogError("[CustomizingManager] 초기화되지 않음");
            return;
        }

        var dto = repository.Load();

        if (dto != null)
        {
            customizing.RestoreFromDTO(dto);
        }
        else
        {
            customizing.InitializeWithDefaults();
        }

        ApplyAllToPlayer();

        Debug.Log("[CustomizingManager] 로드 완료");
        OnLoaded?.Invoke();
    }

    // 현재 상태 저장
    public void Save()
    {
        if (customizing == null)
        {
            Debug.LogError("[CustomizingManager] 초기화되지 않음");
            return;
        }

        var dto = customizing.ToDTO();
        bool success = repository.Save(dto);

        if (success)
        {
            Debug.Log("[CustomizingManager] 저장 완료");
            OnSaved?.Invoke();
        }
    }

    // 아이템 선택 (UI에서 호출)
    public bool SelectItem(CustomizingItemSO item)
    {
        if (customizing == null || item == null) return false;

        bool success = customizing.Equip(item);

        if (success)
        {
            // 플레이어에 즉시 반영
            ApplyToPlayer(item.CustomizingType);
        }

        return success;
    }

    /// ID로 아이템 선택
    public bool SelectItemById(string itemId)
    {
        var item = catalog.GetItemById(itemId);
        return SelectItem(item);
    }

    public void ResetAll()
    {
        customizing?.ResetToDefaults();
        ApplyAllToPlayer();
    }

    // 현재 장착된 아이템 가져오기
    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        return customizing?.GetEquipped(type);
    }

    // 특정 종류의 아이템 목록 가져오기
    public List<CustomizingItemSO> GetItemsByType(CustomizingType type)
    {
        return catalog?.GetItemsByType(type) ?? new List<CustomizingItemSO>();
    }

    // 특정 종류의 잠금 해제된 아이템 목록 가져오기
    public List<CustomizingItemSO> GetUnlockedItemsByType(CustomizingType type)
    {
        return catalog?.GetUnlockedItemsByType(type) ?? new List<CustomizingItemSO>();
    }

    // 특정 아이템이 현재 장착 중인지 확인
    public bool IsEquipped(CustomizingItemSO item)
    {
        return customizing?.IsEquipped(item) ?? false;
    }

    // ========== Private Methods ==========

    private void HandleItemEquipped(CustomizingType type, CustomizingItemSO item)
    {
        OnItemChanged?.Invoke(type, item);
    }

    private void HandleItemUnequipped(CustomizingType type)
    {
        OnItemChanged?.Invoke(type, null);
    }

    private void ApplyToPlayer(CustomizingType type)
    {
        if (customizingPlayer == null) return;

        var item = customizing.GetEquipped(type);
        customizingPlayer.ApplyItem(type, item);
    }

    private void ApplyAllToPlayer()
    {
        if (customizingPlayer == null) return;

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            var item = customizing.GetEquipped(type);
            customizingPlayer.ApplyItem(type, item);
        }
    }

    private void OnDestroy()
    {
        if (customizing != null)
        {
            customizing.OnItemEquipped -= HandleItemEquipped;
            customizing.OnItemUnequipped -= HandleItemUnequipped;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
