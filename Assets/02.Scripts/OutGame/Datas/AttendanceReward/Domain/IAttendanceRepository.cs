using Cysharp.Threading.Tasks;

public interface IAttendanceRepository
{
    UniTask<AttendanceRecord> LoadAsync(string playerId);
    UniTask SaveAsync(string playerId, AttendanceRecord record);
}