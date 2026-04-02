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

    private Customizing _domain;
    private ICustomizingRepository _repository;
    private CustomizingState _savedState;
    private CustomizingSaveData _currentSaveData;

    private CustomizingSlotManager _slotManager;
    private CustomizingUnlockManager _unlockManager;

    public event Action OnInitialized;
    public event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    public event Action OnSaved;
    public event Action OnLoaded;
    public event Action<string> OnItemUnlocked;
    public event Action OnAppearanceApplied;

    // 슬롯 이벤트
    public event Action OnSlotLoaded;
    public event Action<int> OnSlotSelected;
    public event Action<int> OnSlotSaved;
    public event Action<int, string> OnSlotNameChanged;

    public bool IsInitialized => _domain != null;                           // 도메인이 생성됐으면 초기화된 걸로 판단
    public int SelectedSlotIndex => _slotManager?.SelectedSlotIndex ?? 0;   // 현재 슬롯 인덱스는 _slotManager가 관리
    public int SlotCount => CustomizingSaveData.MaxSlotCount;
    public bool IsSlotLoaded => _currentSaveData != null;

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
        Load();
    }

    public void Initialize()
    {
        if (_catalog == null) return;
        if (_baseEquipmentCatalog == null) return;

        _catalog.Initialize();
        _baseEquipmentCatalog.Initialize();

        _repository = new LocalCustomizingRepository(_userId);
        _domain = new Customizing(_catalog);
        _savedState = new CustomizingState();

        // 슬롯 매니저 초기화
        _slotManager = new CustomizingSlotManager(
            _domain,
            _repository,
            () => _currentSaveData,
            () => _domain.CopyStateTo(_savedState)
        );
        _slotManager.OnSlotSelected += index => OnSlotSelected?.Invoke(index);
        _slotManager.OnSlotSaved += index => OnSlotSaved?.Invoke(index);
        _slotManager.OnSlotNameChanged += (index, name) => OnSlotNameChanged?.Invoke(index, name);
        _slotManager.OnSlotApplied += () => OnLoaded?.Invoke();

        // 해금 매니저 초기화
        _unlockManager = new CustomizingUnlockManager(
            _domain,
            _catalog,
            _repository,
            _attendanceRewardTable,
            () => _currentSaveData,
            data => _currentSaveData = data
        );
        _unlockManager.OnItemUnlocked += itemId => OnItemUnlocked?.Invoke(itemId);

        OnInitialized?.Invoke();
    }

    // ========== 저장/로드 ==========

    public void Load()
    {
        LoadAsync().Forget();
    }

    public async UniTask LoadAsync()
    {
        if (_domain == null) return;

        _currentSaveData = await _repository.Load();

        if (_currentSaveData != null && _currentSaveData.SelectedItems.Count > 0)
        {
            _domain.RestoreFromSaveData(_currentSaveData);
        }
        else
        {
            _domain.InitializeWithDefaults();
        }

        _domain.CopyStateTo(_savedState);   // 지금 상태를 세이브 데이터에 복사하고
        OnLoaded?.Invoke();                 // UI를 갱신하고
        OnSlotLoaded?.Invoke();             // 슬롯 로드 완료 알림
    }

    public void Save()
    {
        if (_domain == null) return;

        _domain.CopyStateTo(_savedState);

        var saveData = _domain.ToSaveData();
        saveData.MergeMetaFrom(_currentSaveData);

        _currentSaveData = saveData;
        _repository.Save(saveData).Forget();
        OnSaved?.Invoke();
    }

    // ========== 아이템 선택/토글 ==========

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

    public EEquipResult ToggleItem(CustomizingItemSO item)
    {
        if (_domain == null) return EEquipResult.InvalidItem;
        if (item == null) return EEquipResult.InvalidItem;

        var result = _domain.ToggleEquip(item);

        if (result.HasChanged())
        {
            if (result == EEquipResult.Equipped)
            {
                OnItemChanged?.Invoke(item.Category, item);
            }
            else if (result == EEquipResult.Unequipped)
            {
                OnItemChanged?.Invoke(item.Category, null);
            }
        }

        return result;
    }

    public EEquipResult SelectItemById(string itemId)
    {
        var item = _catalog.GetItemById(itemId);
        return SelectItem(item);
    }

    // ========== UI 상태 관리 ==========

    public void OpenCustomizingUI() => ResetToSaved();

    public void CloseCustomizingUI()
    {
        ResetToSaved();
        OnAppearanceApplied?.Invoke();
    }

    public void ResetToSaved()
    {
        if (_domain == null) return;
        _domain.RestoreFromState(_savedState);
        OnLoaded?.Invoke();
    }

    // ========== 슬롯 API (위임) ==========

    public void SelectSlot(int index) => _slotManager?.SelectSlot(index);
    public void SaveToSelectedSlot() => _slotManager?.SaveToSelectedSlot();
    public void SaveToSlot(int index) => _slotManager?.SaveToSlot(index);
    public void LoadFromSlot(int index) => _slotManager?.LoadFromSlot(index);
    public void SetSlotName(int index, string name) => _slotManager?.SetSlotName(index, name);
    public string GetSlotName(int index) => _slotManager?.GetSlotName(index) ?? $"Slot {index + 1}";
    public CustomizingSlotData GetSlot(int index) => _slotManager?.GetSlot(index);
    public IReadOnlyList<CustomizingSlotData> GetAllSlots() => _slotManager?.GetAllSlots();
    public bool IsSlotEmpty(int index) => _slotManager?.IsSlotEmpty(index) ?? true;
    public int FindMatchingSlot() => _slotManager?.FindMatchingSlot() ?? -1;
    public void AutoSelectSlot() => _slotManager?.AutoSelectSlot();

    // ========== 해금 API (위임) ==========

    public bool IsItemLocked(string itemId) => _unlockManager?.IsItemLocked(itemId) ?? false;
    public void UnlockItem(string itemId) => _unlockManager?.UnlockItem(itemId);
    public bool HasLockedEquippedItems() => _unlockManager?.HasLockedEquippedItems() ?? false;

    // ========== 조회 API ==========

    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        return _domain?.GetEquipped(type);
    }

    public Dictionary<CustomizingType, string> GetEquippedItemIds()
    {
        var result = new Dictionary<CustomizingType, string>();
        if (_domain == null) return result;

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            if (type == CustomizingType.None) continue;

            var item = _domain.GetEquipped(type);
            if (item != null)
                result[type] = item.ItemId;
        }

        return result;
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

    public bool HasUnsavedChanges()
    {
        if (_domain == null || _slotManager == null) return false;

        var selectedSlot = _slotManager.GetSlot(_slotManager.SelectedSlotIndex);
        if (selectedSlot == null || selectedSlot.IsEmpty()) return true;

        return !_domain.MatchesSlotData(selectedSlot);
    }
}
