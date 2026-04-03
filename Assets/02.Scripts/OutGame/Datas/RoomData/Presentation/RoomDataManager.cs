using System;
using System.Collections.Generic;
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
    private int _selectedStageIndex;

    private const byte HOSPITAL_UPGRADE_EVENT = 101;

    public bool HasRoomWallet => _roomWallet != null;
    public int Star => _roomWallet != null ? _roomWallet.TotalStars : 0;
    public RoomCurrency Coin => _roomWallet != null ? _roomWallet.Coin : RoomCurrency.Default(ERoomCurrencyType.Coin);
    public int SelectedStageIndex => _selectedStageIndex;
    public IReadOnlyList<StageDefinitionSO> StageDefinitions => _stageCatalog != null ? _stageCatalog.StageDefinitions : Array.Empty<StageDefinitionSO>();

    public HospitalLevelDefinitionSO CurrentLevelDefinition => _roomWallet != null ? _hospitalLevelCatalog.GetLevel(_roomWallet.HospitalLevel.Value) : null;
    public HospitalLevelDefinitionSO NextLevelDefinition => _roomWallet != null ? _hospitalLevelCatalog.GetNextLevel(_roomWallet.HospitalLevel.Value) : null;

    public event Action<int, int> OnRoomDataChanged;

    public event Action OnRoomDataLoaded;

    public event Action<HospitalLevelDefinitionSO> OnHospitalUpgraded;
    public event Action<int, StageDefinitionSO> OnSelectedStageChanged;

    // ── 초기화 ────────────────────────────────────────────────────────────
    public void Initialize(IRoomCurrencyRepository roomDataRepository)
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
        _selectedStageIndex = 0;
    }

    // ── 로드 / 저장 ───────────────────────────────────────────────────────
    public void LoadRoomData()
    {
        LoadRoomDataAsync().Forget();
    }

    private async UniTask LoadRoomDataAsync()
    {
        await LoadCurrentRoom(PhotonNetwork.CurrentRoom.Name);
        OnRoomDataLoaded?.Invoke();
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

            SelectHighestAvailableStage();

            SaveData();
        }

        _roomWallet = wallet;


        int selectedStage = RoomProperties.GetSelectedStage();
        if (selectedStage == -1)
        {
            SelectHighestAvailableStage();
        }
        else
        {
            if (_stageCatalog == null || !_stageCatalog.TryGetStageDefinition(selectedStage, out StageDefinitionSO stageDefinition))
            {
                return;
            }
        
            _selectedStageIndex = selectedStage;
            OnSelectedStageChanged?.Invoke(selectedStage, stageDefinition);
        }     
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

    public bool IsStageAvailable(StageDefinitionSO stageDefinition)
    {
        return _roomWallet != null &&
               stageDefinition != null &&
               _roomWallet.IsStageAvailable(stageDefinition);
    }

    public bool TrySelectStage(int stageIndex)
    {

        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        if (_stageCatalog == null || _roomWallet == null)
        {
            return false;
        }

        if (!_stageCatalog.TryGetStageDefinition(stageIndex, out StageDefinitionSO stageDefinition))
        {
            return false;
        }

        if (!_roomWallet.IsStageAvailable(stageDefinition))
        {
            return false;
        }

        if (_selectedStageIndex == stageIndex)
        {
            return true;
        }
  
        RoomProperties.SetSelectedStage(stageIndex);
        return true;
    }


    // ── 병원 업그레이드 ───────────────────────────────────────────────────
    public bool TryUpgradeHospital()
    {
        if (!PhotonNetwork.InRoom) throw new InvalidOperationException("병원 접속 상태가 아닙니다.");

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

        OnHospitalUpgraded?.Invoke(_hospitalLevelCatalog.GetLevel(_roomWallet.HospitalLevel.Value));
        Debug.Log($"[RoomDataManager] 업그레이드 완료 - {CurrentLevelDefinition.HospitalName}");
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

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (!changedProps.TryGetValue(RoomProperties.SelectedStageKey, out object value) || value is not int stageIndex)
        {
            return;
        }

        if (_stageCatalog == null || !_stageCatalog.TryGetStageDefinition(stageIndex, out StageDefinitionSO stageDefinition))
        {
            return;
        }

        if (_selectedStageIndex == stageIndex)
        {
            return;
        }

        int stage = RoomProperties.GetSelectedStage();
        _selectedStageIndex = stage;
        OnSelectedStageChanged?.Invoke(stage, _stageCatalog.StageDefinitions[stage]);
        
    }

    // ── 보상 ──────────────────────────────────────────────────────────────
    public StageReward ApplyReward(string stageId, StageResult result)
    {
        if (!PhotonNetwork.InRoom) throw new InvalidOperationException("병원 접속 상태가 아닙니다.");

        StageStars previousStars = _roomWallet.GetStageStars(stageId);
        StageReward reward = StageRewardCalculator.Calculate(result, previousStars);
        _roomWallet = _roomWallet.ApplyReward(stageId, reward);

        SaveData();
        return reward;
    }
    private void SelectHighestAvailableStage()
    {
        if (_stageCatalog == null || _stageCatalog.StageCount <= 0)
        {
            _selectedStageIndex = 0;
            return;
        }

        int bestStageIndex = 0;
        int highestRequiredHospitalLevel = int.MinValue;

        for (int i = 0; i < _stageCatalog.StageCount; i++)
        {
            if (!_stageCatalog.TryGetStageDefinition(i, out StageDefinitionSO stageDefinition))
            {
                continue;
            }

            if (!_roomWallet.IsStageAvailable(stageDefinition))
            {
                continue;
            }

            if (stageDefinition.RequiredHospitalLevel <= highestRequiredHospitalLevel)
            {
                continue;
            }

            highestRequiredHospitalLevel = stageDefinition.RequiredHospitalLevel;
            bestStageIndex = i;
        }

        if (_selectedStageIndex == bestStageIndex)
        {
            return;
        }

        RoomProperties.SetSelectedStage(bestStageIndex);
    }
}
