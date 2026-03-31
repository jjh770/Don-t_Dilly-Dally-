using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttendanceRewardTable",
                 menuName = "Attendance/RewardTable")]
public class AttendanceRewardSO : ScriptableObject, IRewardRepository
{
    [SerializeField] private List<CustomizingItemSO> _rewards;

    public AttendanceReward GetReward(int day)
    {
        int index = day - 1;
        if (index < 0)
        {
            throw new InvalidOperationException("Day는 1보다 작을 수 없습니다.");
        }
        if (index >= _rewards.Count)
        {
            throw new InvalidOperationException("모든 보상을 수령하였습니다.");
        }
        var item = _rewards[index];
        string itemId = item != null ? item.ItemId : "";

        return new AttendanceReward(day, itemId);
    }

    public bool RewardComplete(int day)
    {
        return _rewards.Count <= day;
    }

    public int GetRewardCount()
    {
        return _rewards.Count;
    }

    public bool IsRewardItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;

        foreach (var item in _rewards)
        {
            if (item != null && item.ItemId == itemId)
                return true;
        }
        return false;
    }
}