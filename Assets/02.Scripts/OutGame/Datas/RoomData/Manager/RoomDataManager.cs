using System;
using Cysharp.Threading.Tasks;
using DontDillyDally.StageFlow;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomDataManager : PunPersistentSingleton<RoomDataManager>
{
    [SerializeField] private StageCatalogSO _stageCatalog;
    [SerializeField] private HospitalLevelCatalogSO _hospitalLevelCatalog;

    private IRoomCurrencyRepository _roomDataRepository;
    private RoomWallet _roomWallet;
    private string _currentRoomCode;

    private const byte HOSPITAL_UPGRADE_EVENT = 101;

    public int Star => _roomWallet.TotalStars;
    public RoomCurrency Coin => _roomWallet.Coin;

    public string CurrentLevelName => _roomWallet != null ? _hospitalLevelCatalog.GetLevel(_roomWallet.HospitalLevel.Value).HospitalName : null;

    public event Action<int, int> OnRoomDataChanged;

    // ── 초기화 ────────────────────────────────────────────────────────────
    public void Initialized(IRoomCurrencyRepository roomDataRepository)
    {
        _roomDataRepository = roomDataRepository;
    }

    // ── Photon 콜백 ───────────────────────────────────────────────────────
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
    }

    public override void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
        base.OnDisable();
    }

    public override void OnJoinedRoom()
    {
        LoadRoomDataAsync().Forget();
    }

    public override void OnLeftRoom()
    {
        _currentRoomCode = null;
        _roomWallet = null;
    }

    // ── 로드 / 저장 ───────────────────────────────────────────────────────
    public void LoadRoomData()
    {
        LoadRoomDataAsync().Forget();
    }

    private async UniTask LoadRoomDataAsync()
    {
        await LoadCurrentRoom(PhotonNetwork.CurrentRoom.Name);
        OnRoomDataChanged?.Invoke(Coin.Value, Star);
    }

    private async UniTask LoadCurrentRoom(string roomCode)
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
        return await _roomDataRepository.IsExist(roomCode);
    }


    // ── 병원 업그레이드 ───────────────────────────────────────────────────
    public bool TryUpgradeHospital()
    {
        if (!PhotonNetwork.InRoom) throw new Exception("병원 접속 상태가 아닙니다.");

        HospitalLevelDefinitionSO next = _hospitalLevelCatalog.GetNextLevel(_roomWallet.HospitalLevel.Value);

        if (next == null)
        {
            Debug.Log("[RoomDataManager] 이미 최고 레벨입니다.");
            return false;
        }

        if (_roomWallet.TotalStars < next.RequiredStars || _roomWallet.Coin.Value < next.UpgradeCost)
        {
            Debug.Log("[RoomDataManager] 업그레이드 조건이 부족합니다.");
            return false;
        }

        PhotonNetwork.RaiseEvent(
            HOSPITAL_UPGRADE_EVENT,
            new object[] { next.UpgradeCost },
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable
        );

        UpgradeHospital(next.UpgradeCost); 
        SaveData();
        return true;
    }

    private void UpgradeHospital(int cost)
    {
        _roomWallet = _roomWallet.UpgradeHospital(cost);
        OnRoomDataChanged?.Invoke(Coin.Value, Star);
        Debug.Log($"[RoomDataManager] {CurrentLevelName}로 업그레이드 완료");
    }


    // ── 이벤트 수신 ───────────────────────────────────────────────────────
    private void OnEventReceived(EventData data)
    {
        if (data.CustomData is not object[] payload) return;

        switch (data.Code)
        {
            case HOSPITAL_UPGRADE_EVENT:
                int upgradeCost = Convert.ToInt32(payload[0]);
                UpgradeHospital(upgradeCost);
                break;
        }
    }

    // ── 보상 ──────────────────────────────────────────────────────────────
    public StageReward ApplyReward(string stageId, StageResult result)
    {
        if (!PhotonNetwork.InRoom) throw new Exception("병원 접속 상태가 아닙니다.");

        StageStars previousStars = _roomWallet.GetStageStars(stageId);
        StageReward reward = StageRewardCalculator.Calculate(result, previousStars);
        _roomWallet = _roomWallet.ApplyReward(stageId, reward);

        SaveData();
        return reward;
    }
}
