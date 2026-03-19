using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;


public class RoomDataManager : PunPersistentSingleton<RoomDataManager>
{
    private IRoomCurrencyRepository _roomDataRepository;

    private RoomWallet _roomWallet;

    private string _currentRoomCode;

    public void Initialized(IRoomCurrencyRepository roomDataRepository)
    {
        _roomDataRepository = roomDataRepository;
    }

    public async UniTask LoadCurrentRoom(string roomCode)
    {
        if (_roomDataRepository == null) return;
        _currentRoomCode = roomCode;

        RoomWallet wallet = await _roomDataRepository.Load(roomCode);

        if (wallet == null)
        {
            Debug.Log("[RoomDataManager] 새로운 데이터를 생성합니다.");
            _roomWallet = RoomWallet.Default;
            
            SaveData();
            return;  
        }

        _roomWallet = wallet;

    }

    private void SaveData()
    {
        if (_roomDataRepository == null) return;

        _roomDataRepository.Save(_currentRoomCode, _roomWallet);
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
