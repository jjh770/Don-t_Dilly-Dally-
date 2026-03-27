using UnityEngine;

public readonly struct AttendanceReward
{
    public int Day { get; }
    public string ItemId { get; }

    public AttendanceReward(int day, string itemId = "")
    {
        Day = day;
        ItemId = itemId;
    }
}