using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerDataManager : PunPersistentSingleton<PlayerDataManager>
{
    private IPlayerInformationRepository _playerRoomRepository;

    private PlayerInformation _playerInformation;

    private string _currentAccount;

    public PlayerInformation PlayerInformation => _playerInformation;

    public event Action OnDataManagerReady;

    public bool IsReady { get; private set; }
    public void Initialized(IPlayerInformationRepository playerRoomRepository)
    {
        _playerRoomRepository = playerRoomRepository;

        InitializeDataAsync().Forget();
    }

    private async UniTask InitializeDataAsync()
    {
        await LoadPlayerInformation("Player");
        IsReady = true;
        OnDataManagerReady?.Invoke();   
    }

    //닉넴 변경 이벤트 구현 필요

    private async UniTask LoadPlayerInformation(string account)
    {
        if (_playerRoomRepository == null) return;
        _currentAccount = account;

        PlayerInformation information = await _playerRoomRepository.Load(account);

        if (information == null)
        {
            Debug.Log("[PlayerDataManager] 새로운 데이터를 생성합니다.");
            _playerInformation = PlayerInformation.Default;

            SaveData();
            return;
        }
        _playerInformation = information;

        Debug.Log("[PlayerDataManager] " +
            "MyName : " + _playerInformation.Name + "\n" +
            "MyHospitals : " +
                string.Join(", ",
                _playerInformation.MyHospitals
                .Select(hospital => $"{hospital.Name} ({hospital.Time.ToLocalTime():yyyy-MM-dd HH:mm})")));
    }

    private void SaveData()
    {
        if (_playerRoomRepository == null) return;

        _playerRoomRepository.Save(_currentAccount, _playerInformation);
    }

    public MyHospital[] GetHospital()
    {
        return _playerInformation.MyHospitals;
    }

    public bool CanAddHospital(string roomCode)
    {
        return _playerInformation.CanAdd(roomCode);
    }

    public void DeleteHospital(string roomCode)
    {
        try
        {
            _playerInformation.TryRemoveHospital(roomCode);
        }
        catch(Exception e) 
        { 
            Debug.Log(e);
        }
    }

    public override void OnJoinedRoom()
    {
        try
        {
            _playerInformation.TryAddHospital(new MyHospital(PhotonServerManager.Instance.RoomCode, DateTime.Now));
            SaveData();
        } 
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayerDataManager] {ex}");
        }
    }

    public override void OnLeftRoom()
    {
        SaveData();
    }
}
