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

    [Header("세팅")]
    [SerializeField] private string _userId = "local_user";
    [SerializeField] private bool _autoLoadOnStart = true;

    private Customizing _domain;
    private ICustomizingRepository _repository;
    private CustomizingState _savedState;

    public event Action OnInitialized;
    public event Action<CustomizingType, CustomizingItemSO> OnItemChanged;
    public event Action OnSaved;
    public event Action OnLoaded;

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

        var saveData = await _repository.Load();

        if (saveData != null && saveData.SelectedItems.Count > 0)
            _domain.RestoreFromSaveData(saveData);
        else
            _domain.InitializeWithDefaults();

        _savedState.CopyFrom(_domain.State);

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

        _savedState.CopyFrom(_domain.State);

        var saveData = _domain.ToSaveData();
        saveData.LastSavedAt = DateTime.UtcNow.ToString("o");
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

    public BaseEquipmentItemSO GetBaseEquipmentItem(BaseEquipmentType type)
    {
        return _baseEquipmentCatalog?.GetItem(type);
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
