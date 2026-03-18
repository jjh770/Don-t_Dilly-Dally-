using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;


public class RoomDataManager : PunPersistentSingleton<RoomDataManager>
{
    private IRoomDataRepository _roomDataRepository;

    private RoomData _roomData;

    private string _currentRoomCode;

    private async void Start()
    {
        await WaitForFirebaseAsync();

        // Repository 생성
        Debug.Log("Data 조회 가능");
        IRoomDataRepository roomDataRepository = new RoomDataFirebaseRepository();
        Initialized(roomDataRepository);
    }

    private async UniTask WaitForFirebaseAsync()
    {
        // FirebaseManager가 준비될 때까지 대기
        while (FirebaseInitializer.Instance == null ||
               !FirebaseInitializer.Instance.IsInitialized)
        {
            await UniTask.Yield();
        }
    }

    public void Initialized(IRoomDataRepository roomDataRepository)
    {
        _roomDataRepository = roomDataRepository;
    }

    public async UniTask LoadCurrentRoom(string roomCode)
    {
        if (_roomDataRepository == null) return;
        _currentRoomCode = roomCode;

        RoomSaveData data = await _roomDataRepository.Load(roomCode);

        if (data == null)
        {
            Debug.Log("[RoomDataManager] 새로운 데이터를 생성합니다.");
            _roomData = new RoomData();
            
            SaveData();
            return;  
        } 

        _roomData = data.RoomData;

    }

    private void SaveData()
    {
        if (_roomDataRepository == null) return;

        RoomSaveData data = new RoomSaveData();
        data.RoomData = _roomData;

        _roomDataRepository.Save(_currentRoomCode, data);
    }

    public async UniTask<bool> IsRoomDataExist(string roomCode)
    {
        if (_roomDataRepository == null) return false;

        bool isExist = await _roomDataRepository.IsExist(roomCode);
        return isExist;
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
