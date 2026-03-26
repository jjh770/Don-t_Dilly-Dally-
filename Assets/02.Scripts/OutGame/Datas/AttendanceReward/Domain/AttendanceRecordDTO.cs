using Firebase.Firestore;

[FirestoreData]
public class AttendanceRecordDTO
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
    public AttendanceRecord ToDomain(string playerID) => new AttendanceRecord(playerID, TotalDays, LastCheckedDate);


    // Domain → DTO
    public static AttendanceRecordDTO FromDomain(AttendanceRecord record) => new AttendanceRecordDTO
    (
        record.TotalDays,
        record.LastCheckedDate
    );
}
