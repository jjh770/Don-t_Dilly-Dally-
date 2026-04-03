using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class CustomizingUnlockManager
{
    private readonly Customizing _domain;
    private readonly ICustomizingCatalog _catalog;
    private readonly ICustomizingRepository _repository;
    private readonly AttendanceRewardSO _attendanceRewardTable;
    private readonly Func<CustomizingSaveData> _getSaveData;
    private readonly Action<CustomizingSaveData> _setSaveData;

    public event Action<string> OnItemUnlocked;

    public CustomizingUnlockManager(
        Customizing domain,
        ICustomizingCatalog catalog,
        ICustomizingRepository repository,
        AttendanceRewardSO attendanceRewardTable,
        Func<CustomizingSaveData> getSaveData,
        Action<CustomizingSaveData> setSaveData)
    {
        _domain = domain;
        _catalog = catalog;
        _repository = repository;
        _attendanceRewardTable = attendanceRewardTable;
        _getSaveData = getSaveData;
        _setSaveData = setSaveData;
    }

    public bool IsItemLocked(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) == true) return false;

        // 출석 보상 테이블에 없으면 Lock 아님
        if ((_attendanceRewardTable?.IsRewardItem(itemId) ?? false) == false)
            return false;

        // 출석 보상 아이템이지만 이미 해금되었으면 Lock 아님
        var saveData = _getSaveData();
        return (saveData?.IsUnlocked(itemId) ?? false) == false;
    }

    public void UnlockItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) == true)
        {
            Debug.LogWarning("[CustomizingUnlockManager] 빈 ItemId로 해금 시도");
            return;
        }

        var saveData = _getSaveData();
        if (saveData == null)
        {
            saveData = CustomizingSaveData.Default;
            _setSaveData(saveData);
        }

        if (saveData.IsUnlocked(itemId) == true)
        {
            Debug.Log($"[CustomizingUnlockManager] 이미 해금된 아이템: {itemId}");
            return;
        }

        var item = _catalog?.GetItemById(itemId);
        if (item == null)
        {
            Debug.LogWarning($"[CustomizingUnlockManager] 카탈로그에 없는 아이템: {itemId}");
            return;
        }

        saveData.TryUnlock(itemId);
        saveData.LastSavedAt = DateTime.UtcNow.ToString("o");
        _repository.Save(saveData).Forget();

        Debug.Log($"[CustomizingUnlockManager] 아이템 해금 완료: {itemId}");
        OnItemUnlocked?.Invoke(itemId);
    }

    public bool HasLockedEquippedItems()
    {
        if (_domain == null) return false;

        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            if (type == CustomizingType.None) continue;

            var item = _domain.GetEquipped(type);
            if (item != null && IsItemLocked(item.ItemId) == true)
                return true;
        }

        return false;
    }
}
