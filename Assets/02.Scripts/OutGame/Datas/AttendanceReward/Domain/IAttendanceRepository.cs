using Cysharp.Threading.Tasks;

public interface IAttendanceRepository
{
    UniTask<AttendanceRecord> LoadAsync();
    UniTask SaveAsync(AttendanceRecord record);
}