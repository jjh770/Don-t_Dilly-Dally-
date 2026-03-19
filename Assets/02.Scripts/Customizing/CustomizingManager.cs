using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 커스터마이징 시스템의 중앙 컨트롤러
/// UI, 도메인, 저장소, 플레이어 뷰를 연결하는 창구 역할
///
/// 책임:
/// - 전체 흐름 조율
/// - 도메인과 UI 사이의 중재
/// - 저장/로드 타이밍 제어
/// - 플레이어 미리보기 갱신 호출
/// </summary>
public class CustomizingManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("아이템 카탈로그 SO")]
    [SerializeField] private CustomizingCatalogSO catalog;

    [Tooltip("커스터마이징 결과를 적용할 플레이어")]
    [SerializeField] private CustomizingPlayer customizingPlayer;

    [Header("Settings")]
    [Tooltip("PlayerPrefs 사용 여부 (false면 파일 저장)")]
    [SerializeField] private bool usePlayerPrefs = false;

    [Tooltip("시작 시 자동 로드")]
    [SerializeField] private bool autoLoadOnStart = true;

    // 도메인
    private Customizing customizing;

    // 저장소
    private CustomizingRepository repository;

    // 이벤트
    public event Action OnInitialized;
    public event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    public event Action OnSaved;
    public event Action OnLoaded;

    // 싱글톤 (선택적)
    public static CustomizingManager Instance { get; private set; }

    /// <summary>
    /// 현재 도메인 상태
    /// </summary>
    public Customizing Domain => customizing;

    /// <summary>
    /// 아이템 카탈로그
    /// </summary>
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

    /// <summary>
    /// 시스템 초기화
    /// </summary>
    public void Initialize()
    {
        if (catalog == null)
        {
            Debug.LogError("[CustomizingManager] Catalog is not assigned!");
            return;
        }

        // 카탈로그 초기화
        catalog.Initialize();

        // 저장소 생성
        repository = new CustomizingRepository(usePlayerPrefs);

        // 도메인 생성
        customizing = new Customizing(catalog);

        // 도메인 이벤트 구독
        customizing.OnItemEquipped += HandleItemEquipped;
        customizing.OnItemUnequipped += HandleItemUnequipped;

        Debug.Log("[CustomizingManager] Initialized");
        OnInitialized?.Invoke();
    }

    /// <summary>
    /// 저장된 데이터 로드
    /// </summary>
    public void Load()
    {
        if (customizing == null)
        {
            Debug.LogError("[CustomizingManager] Not initialized");
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

        // 플레이어에 전체 상태 적용
        ApplyAllToPlayer();

        Debug.Log("[CustomizingManager] Loaded");
        OnLoaded?.Invoke();
    }

    /// <summary>
    /// 현재 상태 저장
    /// </summary>
    public void Save()
    {
        if (customizing == null)
        {
            Debug.LogError("[CustomizingManager] Not initialized");
            return;
        }

        var dto = customizing.ToDTO();
        bool success = repository.Save(dto);

        if (success)
        {
            Debug.Log("[CustomizingManager] Saved");
            OnSaved?.Invoke();
        }
    }

    /// <summary>
    /// 아이템 선택 (UI에서 호출)
    /// </summary>
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

    /// <summary>
    /// ID로 아이템 선택
    /// </summary>
    public bool SelectItemById(string itemId)
    {
        var item = catalog.GetItemById(itemId);
        return SelectItem(item);
    }

    /// <summary>
    /// 특정 종류 초기화 (기본값으로)
    /// </summary>
    public void ResetType(CustomizingType type)
    {
        customizing?.Unequip(type);
        ApplyToPlayer(type);
    }

    /// <summary>
    /// 전체 초기화 (기본값으로)
    /// </summary>
    public void ResetAll()
    {
        customizing?.ResetToDefaults();
        ApplyAllToPlayer();
    }

    /// <summary>
    /// 현재 장착된 아이템 가져오기
    /// </summary>
    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        return customizing?.GetEquipped(type);
    }

    /// <summary>
    /// 특정 종류의 아이템 목록 가져오기
    /// </summary>
    public List<CustomizingItemSO> GetItemsByType(CustomizingType type)
    {
        return catalog?.GetItemsByType(type) ?? new List<CustomizingItemSO>();
    }

    /// <summary>
    /// 특정 종류의 잠금 해제된 아이템 목록 가져오기
    /// </summary>
    public List<CustomizingItemSO> GetUnlockedItemsByType(CustomizingType type)
    {
        return catalog?.GetUnlockedItemsByType(type) ?? new List<CustomizingItemSO>();
    }

    /// <summary>
    /// 특정 아이템이 현재 장착 중인지 확인
    /// </summary>
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

    /// <summary>
    /// 특정 종류를 플레이어에 적용
    /// </summary>
    private void ApplyToPlayer(CustomizingType type)
    {
        if (customizingPlayer == null) return;

        var item = customizing.GetEquipped(type);
        customizingPlayer.ApplyItem(type, item);
    }

    /// <summary>
    /// 모든 종류를 플레이어에 적용
    /// </summary>
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
