using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseAttendanceRepository : IAttendanceRepository
{
    private readonly FirebaseFirestore _db;

    private static string COLLECTION_NAME = "AttendanceRecord";
    public FirebaseAttendanceRepository(FirebaseFirestore db)
    {
        _db = db;
    }

    public async UniTask<AttendanceRecord> LoadAsync(string playerId)
    {
        try
        {
            var result = await _db.Collection(COLLECTION_NAME).Document(playerId).GetSnapshotAsync();

            AttendanceRecordDTO dto = result.ConvertTo<AttendanceRecordDTO>();

            Debug.LogFormat("[FirebaseAttendanceRepository] 불러오기 성공");
            if (dto == null)
            {
                Debug.LogWarning("[FirebaseAttendanceRepository] 불러온 데이터가 null 입니다. null을 반환합니다.");
                return null;
            }
            return dto.ToDomain(playerId);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[FirebaseAttendanceRepository] 불러오기 실패, null을 반환합니다. :" + e);
            return null;
        }
    }

    public async UniTask SaveAsync(string playerId, AttendanceRecord record)
    {
        try
        {
            var dto = AttendanceRecordDTO.FromDomain(record);
            await _db.Collection(COLLECTION_NAME).Document(playerId).SetAsync(dto);
            Debug.Log("[FirebaseAttendanceRepository] 저장 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[FirebaseAttendanceRepository] 저장 실패: " + e);
        }
    }
}