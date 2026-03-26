using System;
using UnityEngine;

public class AttendanceRecord
{
    public string PlayerId { get; }
    public int TotalDays { get; private set; }
    public string LastCheckedDate { get; private set; }

    public AttendanceRecord(string playerId)
    {
        PlayerId = playerId;
    }

    public AttendanceRecord(string playerId, int totalDays, string lastCheckedDate)
    {
        PlayerId = playerId;
        TotalDays = totalDays;
        LastCheckedDate = lastCheckedDate;
    }

    public bool CanCheckToday()
    {
        return LastCheckedDate != DateTime.Now.ToString("yyyy-MM-dd");
    }


    public void MarkChecked()
    {
        if (!CanCheckToday())
            throw new InvalidOperationException("오늘 이미 출석 체크를 완료했습니다.");

        TotalDays++;
        LastCheckedDate = DateTime.Now.ToString("yyyy-MM-dd");
    }
}