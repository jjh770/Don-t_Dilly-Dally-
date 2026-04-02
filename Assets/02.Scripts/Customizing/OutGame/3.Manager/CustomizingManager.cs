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

    // 슬롯 관련
    private int _selectedSlotIndex = 0;

    public event Action OnInitialized;
    public event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    public event Action OnSaved;
    public event Action OnLoaded;
    public event Action<string> OnItemUnlocked;

    // 슬롯 이벤트
    public event Action OnSlotLoaded;
    public event Action<int> OnSlotSelected;
    public event Action<int> OnSlotSaved;
    public event Action<int, string> OnSlotNameChanged;

    public bool IsInitialized => _domain != null;
    public int SelectedSlotIndex => _selectedSlotIndex;
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

        // 커스터마이징 데이터 로드
        _currentSaveData = await _repository.Load();

        if (_currentSaveData != null && _currentSaveData.SelectedItems.Count > 0)
            _domain.RestoreFromSaveData(_currentSaveData);
        else
            _domain.InitializeWithDefaults();

        _savedState.CopyFrom(_domain.State);

        Debug.Log($"[CustomizingManager] 로드 완료. 해금된 아이템 수: {_currentSaveData?.UnlockedItems?.Count ?? 0}, 슬롯 수: {_currentSaveData?.Slots?.Count ?? 0}");
        OnLoaded?.Invoke();
        OnSlotLoaded?.Invoke();
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

        // 기존 해금 및 슬롯 데이터 유지
        if (_currentSaveData != null)
        {
            foreach (var itemId in _currentSaveData.UnlockedItems)
            {
                saveData.UnlockedItems.Add(itemId);
            }
            foreach (var slot in _currentSaveData.Slots)
            {
                saveData.Slots.Add(slot);
            }
            saveData.EnsureSlots();
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

    // ========== 슬롯 API ==========

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
            return;

        _selectedSlotIndex = index;
        OnSlotSelected?.Invoke(index);
    }

    public void SaveToSelectedSlot()
    {
        SaveToSlot(_selectedSlotIndex);
    }

    public void SaveToSlot(int index)
    {
        if (_currentSaveData == null || index < 0 || index >= SlotCount)
            return;

        var slotData = CreateSlotDataFromCurrentState();
        if (slotData == null)
            return;

        var existingSlot = _currentSaveData.GetSlot(index);
        slotData.Name = existingSlot?.Name ?? $"Slot {index + 1}";

        _currentSaveData.SetSlot(index, slotData);
        _repository.Save(_currentSaveData).Forget();

        Debug.Log($"[CustomizingManager] 슬롯 {index + 1} 저장 완료");
        OnSlotSaved?.Invoke(index);
    }

    public void LoadFromSlot(int index)
    {
        if (_currentSaveData == null || index < 0 || index >= SlotCount)
            return;

        var slot = _currentSaveData.GetSlot(index);
        if (slot == null || slot.IsEmpty())
            return;

        ApplySlotData(slot);
    }

    public void SetSlotName(int index, string name)
    {
        if (_currentSaveData == null || index < 0 || index >= SlotCount)
            return;

        var slot = _currentSaveData.GetSlot(index);
        if (slot == null)
            return;

        slot.Name = name;
        _repository.Save(_currentSaveData).Forget();

        OnSlotNameChanged?.Invoke(index, name);
    }

    public string GetSlotName(int index)
    {
        if (_currentSaveData == null || index < 0 || index >= SlotCount)
            return $"Slot {index + 1}";

        var slot = _currentSaveData.GetSlot(index);
        return slot?.Name ?? $"Slot {index + 1}";
    }

    public CustomizingSlotData GetSlot(int index)
    {
        return _currentSaveData?.GetSlot(index);
    }

    public IReadOnlyList<CustomizingSlotData> GetAllSlots()
    {
        return _currentSaveData?.Slots;
    }

    public bool IsSlotEmpty(int index)
    {
        var slot = _currentSaveData?.GetSlot(index);
        return slot == null || slot.IsEmpty();
    }

    public int FindMatchingSlot()
    {
        if (_currentSaveData == null || _domain == null)
            return -1;

        for (int i = 0; i < _currentSaveData.Slots.Count; i++)
        {
            var slot = _currentSaveData.Slots[i];
            if (slot != null && !slot.IsEmpty() && slot.Matches(_domain.State.GetAll()))
            {
                return i;
            }
        }

        return -1;
    }

    public void AutoSelectSlot()
    {
        int matchingIndex = FindMatchingSlot();

        if (matchingIndex >= 0)
        {
            SelectSlot(matchingIndex);
        }
        else
        {
            SelectSlot(0);
        }
    }

    private CustomizingSlotData CreateSlotDataFromCurrentState()
    {
        if (_domain == null) return null;

        var slotData = new CustomizingSlotData();
        slotData.CopyFrom(_domain.State.GetAll());
        return slotData;
    }

    private void ApplySlotData(CustomizingSlotData slotData)
    {
        if (_domain == null || slotData == null || slotData.IsEmpty())
            return;

        _domain.State.Clear();

        var equippedItems = slotData.ToEquippedItems();
        foreach (var kvp in equippedItems)
        {
            _domain.State.SetEquipped(kvp.Key, kvp.Value);
        }

        OnLoaded?.Invoke();
    }

    // ========== 조회 API ==========
    public CustomizingItemSO GetEquipped(CustomizingType type)
    {
        var spec = _domain?.GetEquipped(type);
        return spec as CustomizingItemSO;
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
        if (_domain == null) return false;

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            if (type == CustomizingType.None) continue;

            var item = _domain.GetEquipped(type);
            if (item != null && IsItemLocked(item.ItemId))
                return true;
        }

        return false;
    }

    public bool HasUnsavedChanges()
    {
        if (_domain == null || _savedState == null) return false;

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            if (type == CustomizingType.None) continue;

            var currentItem = _domain.GetEquipped(type);
            var savedItemId = _savedState.GetEquippedId(type);

            string currentItemId = currentItem?.ItemId;

            if (currentItemId != savedItemId)
                return true;
        }

        return false;
    }
}
