using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class CustomizingManager : MonoBehaviour, ICustomizingManager
{
    public static CustomizingManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private CustomizingCatalogSO _catalog;
    [SerializeField] private BaseEquipmentCatalogSO _baseEquipmentCatalog;
    [SerializeField] private AttendanceRewardSO _attendanceRewardTable;

    [Header("세팅")]
    [SerializeField] private string _userId = "local_user";
    [SerializeField] private bool _autoLoadOnStart = true;

    private Customizing _domain;
    private ICustomizingRepository _repository;
    private CustomizingState _savedState;
    private CustomizingSaveData _currentSaveData;

    public event Action OnInitialized;
    public event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    public event Action OnSaved;
    public event Action OnLoaded;
    public event Action<string> OnItemUnlocked;

    public bool IsInitialized => _domain != null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

        if (_autoLoadOnStart) Load();
    }

    public void Initialize()
    {
        if (_catalog == null)
        {
            Debug.LogError("[CustomizingManager] 카탈로그가 할당되지 않았습니다.");
            return;
        }
        if (_baseEquipmentCatalog == null)
        {
            Debug.LogError("[CustomizingManager] 기본 장착 카탈로그가 할당되지 않았습니다.");
            return;
        }

        _catalog.Initialize();
        _baseEquipmentCatalog.Initialize();

        _repository = new LocalCustomizingRepository(_userId);
        _domain = new Customizing(_catalog);
        _savedState = new CustomizingState();

        OnInitialized?.Invoke();
    }

    public void Load()
    {
        LoadAsync().Forget();
    }

    public async UniTask LoadAsync()
    {
        if (_domain == null)
        {
            Debug.LogError("[CustomizingManager] 초기화되지 않음");
            return;
        }

        _currentSaveData = await _repository.Load();

        if (_currentSaveData != null && _currentSaveData.SelectedItems.Count > 0)
            _domain.RestoreFromSaveData(_currentSaveData);
        else
            _domain.InitializeWithDefaults();

        _savedState.CopyFrom(_domain.State);

        Debug.Log($"[CustomizingManager] 로드 완료. 해금된 아이템 수: {_currentSaveData?.UnlockedItems?.Count ?? 0}");
        OnLoaded?.Invoke();
    }

    public void Save()
    {
        if (_domain == null)
        {
            Debug.LogError("[CustomizingManager] 초기화되지 않음");
            return;
        }

        _savedState.CopyFrom(_domain.State);

        var saveData = _domain.ToSaveData();
        saveData.LastSavedAt = DateTime.UtcNow.ToString("o");

        // 기존 해금 데이터 유지
        if (_currentSaveData != null)
        {
            foreach (var itemId in _currentSaveData.UnlockedItems)
            {
                saveData.UnlockedItems.Add(itemId);
            }
        }

        _currentSaveData = saveData;
        _repository.Save(saveData).Forget();

        Debug.Log("[CustomizingManager] 저장 완료");
        OnSaved?.Invoke();
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

    public EEquipResult SelectItemById(string itemId)
    {
        var item = _catalog.GetItemById(itemId);
        return SelectItem(item);
    }

    public void OpenCustomizingUI()
    {
        if (_domain == null) return;

        _domain.State.CopyFrom(_savedState);
        Debug.Log("[CustomizingManager] 커스터마이징 UI 열림 - Working State 초기화");
        OnLoaded?.Invoke();
    }

    public void CloseCustomizingUI()
    {
        if (_domain == null) return;

        _domain.State.CopyFrom(_savedState);
        Debug.Log("[CustomizingManager] 커스터마이징 UI 닫힘 - Saved State로 복원");
        OnLoaded?.Invoke();
    }

    public void ResetToSaved()
    {
        if (_domain == null) return;

        _domain.State.CopyFrom(_savedState);
        Debug.Log("[CustomizingManager] Saved State로 리셋");
        OnLoaded?.Invoke();
    }

    public void ResetAll()
    {
        ResetToSaved();
    }

    // ========== 조회 API ==========
    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        var spec = _domain?.GetEquipped(type);
        return spec as CustomizingItemSO;
    }

    public Dictionary<CustomizingType, string> GetEquippedItemIds()
    {
        if (_domain?.State == null)
            return new Dictionary<CustomizingType, string>();

        return new Dictionary<CustomizingType, string>(_domain.State.GetAll());
    }

    public CustomizingItemSO GetItemById(string itemId)
    {
        return _catalog?.GetItemById(itemId);
    }


    public IEnumerable<(BaseEquipmentType type, BaseEquipmentItemSO item)> GetAllBaseEquipmentItems()
    {
        if (_baseEquipmentCatalog == null)
            yield break;

        foreach (BaseEquipmentType type in Enum.GetValues(typeof(BaseEquipmentType)))
        {
            var item = _baseEquipmentCatalog.GetItem(type);
            if (item != null)
                yield return (type, item);
        }
    }

    public List<CustomizingItemSO> GetUnlockedItemsByType(CustomizingType type)
    {
        return _catalog?.GetUnlockedItemsByType(type) ?? new List<CustomizingItemSO>();
    }

    public List<CustomizingItemSO> GetAllItemsByType(CustomizingType type)
    {
        return _catalog?.GetItemsByType(type) ?? new List<CustomizingItemSO>();
    }

    // ========== 해금 API ==========

    // 아이템이 잠금 상태인지 확인
    public bool IsItemLocked(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;

        // 출석 보상 테이블에 없으면 Lock 아님
        if (!(_attendanceRewardTable?.IsRewardItem(itemId) ?? false))
            return false;

        // 출석 보상 아이템이지만 이미 해금되었으면 Lock 아님
        return !(_currentSaveData?.IsUnlocked(itemId) ?? false);
    }

    // 아이템 해금 처리
    public void UnlockItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("[CustomizingManager] 빈 ItemId로 해금 시도");
            return;
        }

        if (_currentSaveData == null)
        {
            _currentSaveData = CustomizingSaveData.Default;
        }

        if (_currentSaveData.IsUnlocked(itemId))
        {
            Debug.Log($"[CustomizingManager] 이미 해금된 아이템: {itemId}");
            return;
        }

        var item = _catalog?.GetItemById(itemId);
        if (item == null)
        {
            Debug.LogWarning($"[CustomizingManager] 카탈로그에 없는 아이템: {itemId}");
            return;
        }

        _currentSaveData.TryUnlock(itemId);
        _currentSaveData.LastSavedAt = DateTime.UtcNow.ToString("o");
        _repository.Save(_currentSaveData).Forget();

        Debug.Log($"[CustomizingManager] 아이템 해금 완료: {itemId}");
        OnItemUnlocked?.Invoke(itemId);
    }

    // 현재 장착 중인 아이템 중 잠금 상태인 것이 있는지 확인
    public bool HasLockedEquippedItems()
    {
        if (_domain?.State == null) return false;

        foreach (var kvp in _domain.State.GetAll())
        {
            if (IsItemLocked(kvp.Value))
                return true;
        }

        return false;
    }

    public bool HasUnsavedChanges()
    {
        if (_domain?.State == null || _savedState == null) return false;

        var currentState = _domain.State.GetAll();
        var savedState = _savedState.GetAll();

        if (currentState.Count != savedState.Count) return true;

        foreach (var kvp in currentState)
        {
            if (!savedState.TryGetValue(kvp.Key, out var savedValue))
                return true;
            if (kvp.Value != savedValue)
                return true;
        }

        return false;
    }
}
