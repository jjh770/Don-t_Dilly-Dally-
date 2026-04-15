using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseAttendanceRepository : IAttendanceRepository
{
    private readonly FirebaseFirestore _db;

    private readonly string _id;

    private const string CollectionName = "users";
    private const string SubCollectionName = "data";
    private const string DataDocumentId = "attendance";
    public FirebaseAttendanceRepository(FirebaseFirestore db, string userId)
    {
        _db = db;
        _id = userId;
    }

    public async UniTask<AttendanceRecord> LoadAsync()
    {
        try
        {
            var result = await _db.Collection(CollectionName)
                          .Document(_id)
                          .Collection(SubCollectionName)
                          .Document(DataDocumentId)
                          .GetSnapshotAsync();

            AttendanceRecordDTO dto = result.ConvertTo<AttendanceRecordDTO>();

            Debug.LogFormat("[FirebaseAttendanceRepository] 불러오기 성공");
            if (dto == null)
            {
                Debug.LogWarning("[FirebaseAttendanceRepository] 불러온 데이터가 null 입니다. null을 반환합니다.");
                return null;
            }
            return dto.ToDomain();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[FirebaseAttendanceRepository] 불러오기 실패, null을 반환합니다. :" + e);
            return null;
        }
    }

    public async UniTask SaveAsync(AttendanceRecord record)
    {
        try
        {
            var dto = AttendanceRecordDTO.FromDomain(record);
            await _db.Collection(CollectionName)
                          .Document(_id)
                          .Collection(SubCollectionName)
                          .Document(DataDocumentId)
                          .SetAsync(dto);
            Debug.Log("[FirebaseAttendanceRepository] 저장 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[FirebaseAttendanceRepository] 저장 실패: " + e);
        }
    }

    [FirestoreData]
    private class AttendanceRecordDTO
    {
        [FirestoreProperty]
        public int TotalDays { get; private set; }

        [FirestoreProperty]
        public string LastCheckedDate { get; private set; }

        public AttendanceRecordDTO() { }

        private AttendanceRecordDTO(int totalDays, string lastCheckedDate)
        {
            TotalDays = totalDays;
            LastCheckedDate = lastCheckedDate;
        }

        // DTO → Domain
        public AttendanceRecord ToDomain() => new AttendanceRecord(TotalDays, LastCheckedDate);


        // Domain → DTO
        public static AttendanceRecordDTO FromDomain(AttendanceRecord record) => new AttendanceRecordDTO
        (
            record.TotalDays,
            record.LastCheckedDate
        );
    }

}

