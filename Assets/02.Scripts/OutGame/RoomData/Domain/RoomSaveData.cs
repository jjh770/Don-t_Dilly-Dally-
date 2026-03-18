
public class RoomSaveData 
{
    public RoomData RoomData { get; set; }

    public static RoomSaveData Default => new RoomSaveData()
    {
        RoomData = new RoomData(0, 0)
    };
}
