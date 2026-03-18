
using Firebase.Firestore;

[FirestoreData]
public class RoomSaveData 
{
    [FirestoreProperty]
    public RoomData RoomData { get; set; }

    public RoomSaveData() { }
    public static RoomSaveData Default => new RoomSaveData()
    {
        RoomData = new RoomData(0, 0, 0)
    };
}
