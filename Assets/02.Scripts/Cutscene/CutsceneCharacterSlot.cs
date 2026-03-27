using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Photon.Realtime;
using UnityEngine;

// 컷씬 캐릭터 슬롯에 할당된 플레이어의 커스터마이징을 적용하는 컴포넌트
// CustomizingCharacterController의 컷씬 전용 경량 버전
[RequireComponent(typeof(CustomizingCharacterView))]
public class CutsceneCharacterSlot : MonoBehaviour
{
    private CustomizingCharacterView _view;
    private ICustomizingAssetLoader _assetLoader;
    private Player _assignedPlayer;

    public bool IsAssigned => _assignedPlayer != null;

    private void Awake()
    {
        _view = GetComponent<CustomizingCharacterView>();
        _assetLoader = new AddressableAssetLoader();
        _view.Initialize(_assetLoader);
    }

    public void AssignPlayer(Player player)
    {
        _assignedPlayer = player;
    }

    // 할당된 플레이어의 Photon CustomProperties에서 커스터마이징 데이터를 읽어 적용
    public async UniTask ApplyCustomizingAsync()
    {
        if (!IsAssigned) return;

        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized)
        {
            Debug.LogWarning("[CutsceneCharacterSlot] CustomizingManager가 준비되지 않음");
            return;
        }

        // Photon CustomProperties에서 아이템 ID 조회
        var itemIds = CustomizingProperties.GetPlayerCustomizing(_assignedPlayer);

        // 기본 장착 적용 (Outfit, Gloves, Pants)
        ApplyBaseEquipment(manager);

        // 커스터마이징 데이터가 없으면 기본 장착만으로 종료
        if (itemIds == null || itemIds.Count == 0) return;

        // 커스터마이징 아이템 비동기 적용
        var tasks = new List<UniTask>();
        foreach (var kvp in itemIds)
        {
            var item = manager.GetItemById(kvp.Value);
            if (item != null)
            {
                tasks.Add(_view.ApplyItemAsync(kvp.Key, item));
            }
        }

        if (tasks.Count > 0)
        {
            await UniTask.WhenAll(tasks);
        }
    }

    // 플레이어 미할당 슬롯용: 기본 장착(Outfit, Gloves, Pants) + 기본 커스터마이징만 적용
    public void ApplyDefaultAppearance()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized)
        {
            Debug.LogWarning("[CutsceneCharacterSlot] CustomizingManager가 준비되지 않음 - 기본 외형 적용 실패");
            return;
        }

        // 기본 장착 적용
        ApplyBaseEquipment(manager);

        // 각 카테고리의 기본 아이템 적용 (IsDefault 플래그 기준)
        _view.ApplyAll(type => FindDefaultItem(manager, type));
    }

    private CustomizingItemSO FindDefaultItem(CustomizingManager manager, CustomizingType type)
    {
        var items = manager.GetUnlockedItemsByType(type);
        if (items == null || items.Count == 0) return null;

        foreach (var item in items)
        {
            if (item.IsDefault) return item;
        }

        return null;
    }

    private void ApplyBaseEquipment(CustomizingManager manager)
    {
        foreach (var (type, item) in manager.GetAllBaseEquipmentItems())
        {
            _view.ApplyBaseEquipment(type, item);
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
        (_assetLoader as IDisposable)?.Dispose();
    }
}
