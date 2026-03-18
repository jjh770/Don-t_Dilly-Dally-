using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;


public class RoomDataManager : PunPersistentSingleton<RoomDataManager>
{
    private IRoomDataRepository _roomDataRepository;

    private RoomData _roomData;

    private string _currentRoomCode;

    private void Start()
    {
        IRoomDataRepository roomDataRepository = new RoomDataFirebaseRepository();
        Initialized(roomDataRepository);
    }

    public void Initialized(IRoomDataRepository roomDataRepository)
    {
        _roomDataRepository = roomDataRepository;
    }

    public async UniTask LoadCurrentRoom(string roomCode)
    {
        _currentRoomCode = roomCode;

        RoomSaveData data = await _roomDataRepository.Load(roomCode);

        if (data == null)
        {
            Debug.Log("[RoomDataManager] 새로운 데이터를 생성합니다.");
            _roomData = new RoomData(0, 0, 0);
            
            SaveData();
            return;  
        } 

        _roomData = data.RoomData;

    }

    private void SaveData()
    {
        RoomSaveData data = new RoomSaveData();
        data.RoomData = _roomData;

        _roomDataRepository.Save(_currentRoomCode, data);
    }

    public async UniTask<bool> IsRoomDataExist(string roomCode)
    {
        RoomSaveData data = await _roomDataRepository.Load(roomCode);
        return data != null;
    }
    
    public override void OnCreatedRoom()
    {
        LoadRoomDataAsync().Forget();
    }

    private async UniTask LoadRoomDataAsync()
    {
        await LoadCurrentRoom(PhotonNetwork.CurrentRoom.Name);
    }
}
