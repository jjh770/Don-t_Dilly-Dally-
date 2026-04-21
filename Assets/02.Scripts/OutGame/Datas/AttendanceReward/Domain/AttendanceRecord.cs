using System;

public interface IAttendanceCheckPolicy
{
    bool CanCheck(string lastCheckedDate, DateTime now);
    string GetCheckedDate(DateTime now);
}

public enum AttendanceCheckUnit
{
    Daily,
    Hourly
}

public static class AttendanceCheckPolicyFactory
{
    public static IAttendanceCheckPolicy Create(AttendanceCheckUnit unit)
    {
        switch (unit)
        {
            case AttendanceCheckUnit.Hourly:
                return new HourlyAttendanceCheckPolicy();
            case AttendanceCheckUnit.Daily:
            default:
                return new DailyAttendanceCheckPolicy();
        }
    }
}

public class DailyAttendanceCheckPolicy : IAttendanceCheckPolicy
{
    private const string DateFormat = "yyyy-MM-dd";

    public bool CanCheck(string lastCheckedDate, DateTime now)
    {
        return lastCheckedDate != GetCheckedDate(now);
    }

    public string GetCheckedDate(DateTime now)
    {
        return now.ToString(DateFormat);
    }
}

public class HourlyAttendanceCheckPolicy : IAttendanceCheckPolicy
{
    private const string DateHourFormat = "yyyy-MM-dd HH";

    public bool CanCheck(string lastCheckedDate, DateTime now)
    {
        return lastCheckedDate != GetCheckedDate(now);
    }

    public string GetCheckedDate(DateTime now)
    {
        return now.ToString(DateHourFormat);
    }
}

public class AttendanceRecord
{
    public int TotalDays { get; private set; }
    public string LastCheckedDate { get; private set; }

    private IAttendanceCheckPolicy _checkPolicy;

    public AttendanceRecord()
        : this(0, null)
    {
    }

    public AttendanceRecord(int totalDays, string lastCheckedDate, IAttendanceCheckPolicy checkPolicy = null)
    {
        TotalDays = totalDays;
        LastCheckedDate = lastCheckedDate;
        _checkPolicy = checkPolicy ?? new DailyAttendanceCheckPolicy();
    }

    public void SetCheckPolicy(IAttendanceCheckPolicy checkPolicy)
    {
        _checkPolicy = checkPolicy ?? new DailyAttendanceCheckPolicy();
    }

    public bool CanCheckToday()
    {
        return CanCheckNow();
    }

    public bool CanCheckNow()
    {
        return _checkPolicy.CanCheck(LastCheckedDate, DateTime.Now);
    }

    public void MarkChecked()
    {
        if (!CanCheckNow())
            throw new InvalidOperationException("Attendance check is already completed for this period.");

        TotalDays++;
        LastCheckedDate = _checkPolicy.GetCheckedDate(DateTime.Now);
    }
}
