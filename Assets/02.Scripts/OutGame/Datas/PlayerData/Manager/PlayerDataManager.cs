using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerDataManager : PunPersistentSingleton<RoomDataManager>
{
    private IPlayerInformationRepository _playerRoomRepository;

    private PlayerInformation _playerInformation;

    private string _currentAccount;

    public void Initialized(IPlayerInformationRepository playerRoomRepository)
    {
        _playerRoomRepository = playerRoomRepository;
        LoadPlayerInformation("Player").Forget();
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
                _playerInformation.GetMyHospitals
                .Select(hospital => $"{hospital.Name} ({hospital.Time.ToLocalTime():yyyy-MM-dd HH:mm})")));
    }

    private void SaveData()
    {
        if (_playerRoomRepository == null) return;

        _playerRoomRepository.Save(_currentAccount, _playerInformation);
    }

    public override void OnJoinedRoom()
    {
        _playerInformation.AddHospital(new MyHospital(PhotonServerManager.Instance.RoomCode, DateTime.Now));
        SaveData();
    }

    public override void OnLeftRoom()
    {
        SaveData();
    }
}
