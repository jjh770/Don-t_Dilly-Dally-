using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttendanceRewardTable",
                 menuName = "Attendance/RewardTable")]
public class AttendanceRewardSO : ScriptableObject, IRewardRepository
{
    [Serializable]
    public class RewardEntry
    {
        [field: SerializeField] public string ItemId {  get; set; }
    }

    [SerializeField] private List<RewardEntry> _rewards;

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
        var entry = _rewards[index];

        return new AttendanceReward(day, entry.ItemId); 
    }

    public bool RewardComplete(int day)
    {
        return _rewards.Count <= day;
    }  
}