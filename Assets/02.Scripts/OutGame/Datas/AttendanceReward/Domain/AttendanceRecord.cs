using System;

public class AttendanceRecord
{
    public int TotalDays { get; private set; }
    public string LastCheckedDate { get; private set; }

    private const string DateFormat = "yyyy-MM-dd";

    public AttendanceRecord() { }

    public AttendanceRecord(int totalDays, string lastCheckedDate)
    {
        TotalDays = totalDays;
        LastCheckedDate = lastCheckedDate;
    }

    public bool CanCheckToday()
    {
        return LastCheckedDate != DateTime.Now.ToString(DateFormat);
    }


    public void MarkChecked()
    {
        if (!CanCheckToday())
            throw new InvalidOperationException("오늘 이미 출석 체크를 완료했습니다.");

        TotalDays++;
        LastCheckedDate = DateTime.Now.ToString(DateFormat);
    }
}
