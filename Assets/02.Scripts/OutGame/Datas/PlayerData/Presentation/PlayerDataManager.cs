using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerDataManager : PunPersistentSingleton<PlayerDataManager>
{
    private IPlayerInformationRepository _playerRoomRepository;

    private PlayerInformation _playerInformation;

    [SerializeField] private string _playerID = "Player";
    [SerializeField] private List<string> _defaultNickNameList = new List<string>
    {
        "김진료",
        "박처방",
        "이수술",
        "최진단",
        "정회복",
        "한응급"
    };

    public string PlayerNickname => _playerInformation.Nickname;

    public string PlayerID => _playerID;

    public PlayerInformation PlayerInformation => _playerInformation;

    public event Action OnDataManagerReady;

    public event Action<string> OnNicknameChanged;
    public bool IsReady { get; private set; }
    public void Initialize(IPlayerInformationRepository playerRoomRepository)
    {
        _playerRoomRepository = playerRoomRepository;

        InitializeDataAsync().Forget();
    }

    private async UniTask InitializeDataAsync()
    {
        await LoadPlayerInformation();
        IsReady = true;
        OnDataManagerReady?.Invoke();   
    }

    //닉넴 변경 이벤트 구현 필요

    private async UniTask LoadPlayerInformation()
    {
        if (_playerRoomRepository == null) return;

        PlayerInformation information = await _playerRoomRepository.Load();

        if (information == null)
        {
            Debug.Log("[PlayerDataManager] 새로운 데이터를 생성합니다.");
            string nickName = _defaultNickNameList.Count > 0
                ? _defaultNickNameList[UnityEngine.Random.Range(0, _defaultNickNameList.Count)]
                : "Player";
            _playerInformation = new PlayerInformation(nickName);
            SaveData();
            return;
        }
        _playerInformation = information;

        Debug.Log("[PlayerDataManager] " +
            "MyName : " + _playerInformation.Nickname + "\n" +
            "MyHospitals : " +
                string.Join(", ",
                _playerInformation.MyHospitals
                .Select(hospital => $"{hospital.Name} ({hospital.Time.ToLocalTime():yyyy-MM-dd HH:mm})")));
    }

    private void SaveData()
    {
        if (_playerRoomRepository == null) return;

        _playerRoomRepository.Save(_playerInformation);
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

    public void SetPlayerID(string id)
    {
        _playerID = id;
    }

    public void ChangeNickname(string nickname)
    {
        _playerInformation.SetName(nickname);
        SaveData();
        OnNicknameChanged?.Invoke(nickname);
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
