using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(PlayerCustomizingView))]
public class LobbyPreviewController : MonoBehaviour
{
    private PlayerCustomizingView _view;
    private bool _isInitialized;

    private void Awake()
    {
        _view = GetComponent<PlayerCustomizingView>();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        var manager = CustomizingManager.Instance;
        if (manager != null)
        {
            manager.OnLoaded += HandleLoaded;
            manager.OnItemChanged += HandleItemChanged;
        }
    }

    private void OnDisable()
    {
        var manager = CustomizingManager.Instance;
        if (manager != null)
        {
            manager.OnLoaded -= HandleLoaded;
            manager.OnItemChanged -= HandleItemChanged;
        }
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        var manager = CustomizingManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[LobbyPreviewController] CustomizingManager가 없습니다. 대기 중...");
            return;
        }

        if (!manager.IsInitialized)
        {
            manager.OnLoaded += OnManagerFirstLoaded;
            return;
        }

        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        await PreloadAllItems();
        ApplyCustomizing();
        SetupPreviewCamera();
        _isInitialized = true;
    }

    private void OnManagerFirstLoaded()
    {
        var manager = CustomizingManager.Instance;
        if (manager != null)
        {
            manager.OnLoaded -= OnManagerFirstLoaded;
        }

        InitializeAsync().Forget();
    }

    private async UniTask PreloadAllItems()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null) return;

        var allItems = new List<CustomizingItemSO>();

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            var items = manager.GetUnlockedItemsByType(type);
            allItems.AddRange(items);
        }

        await _view.PreloadItemsAsync(allItems);
        Debug.Log($"[LobbyPreviewController] {allItems.Count}개 아이템 프리로드 완료");
    }

    private void HandleLoaded()
    {
        ApplyCustomizing();
    }

    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item)
    {
        _view.ApplyItem(type, item);
    }

    private void ApplyCustomizing()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized) return;

        // 기본 장비 적용
        foreach (var (type, item) in manager.GetAllBaseEquipmentItems())
        {
            _view.ApplyBaseEquipment(type, item);
        }

        // 커스터마이징 적용
        _view.ApplyAll(type => manager.GetEquipped(type));

        Debug.Log("[LobbyPreviewController] 커스터마이징 적용 완료");
    }

    private void SetupPreviewCamera()
    {
        CharacterPreviewCamera.SetLocalPlayerTarget(transform);
    }
}
