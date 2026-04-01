using System;
using Cysharp.Threading.Tasks;
using DontDillyDally.StageFlow;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class RoomDataManager : PunPersistentSingleton<RoomDataManager>
{
    [SerializeField] private StageCatalogSO _stageCatalog;
    private IRoomCurrencyRepository _roomDataRepository;

    private RoomWallet _roomWallet;

    private string _currentRoomCode;

    public int Star => _roomWallet.TotalStars;
    public RoomCurrency Coin => _roomWallet.Coin;

    public event Action<int, int> OnRoomDataChanged;
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
            SyncDefaultStages();
            SaveData();
            return;  
        }

        _roomWallet = wallet;
        SyncDefaultStages();
    }

    private void SaveData()
    {
        if (_roomDataRepository == null) return;

        _roomDataRepository.Save(_currentRoomCode, _roomWallet);
    }

    public StageReward ApplyReward(string stageId, StageResult result)
    {
        if (!PhotonNetwork.InRoom) throw new Exception("병원 접속 상태가 아닙니다.");

        StageStars previousStars = _roomWallet.GetStageStars(stageId);
        StageReward reward = StageRewardCalculator.Calculate(result, previousStars);

        _roomWallet = _roomWallet.ApplyReward(stageId, reward);

        SaveData();
        return reward;
    }

    // 방 입장 시 isDefaultUnlocked 스테이지 자동 등록
    private void SyncDefaultStages()
    {
        if (!PhotonNetwork.InRoom) throw new Exception("병원 접속 상태가 아닙니다.");

        bool changed = false;
        foreach (var stage in _stageCatalog.StageDefinitions)
        {
            if (stage.IsDefaultUnlocked && !_roomWallet.IsStageUnlocked(stage.StageId))
            {
                _roomWallet = _roomWallet.UnlockStage(stage.StageId);
                changed = true;
            }
        }
        if (changed) SaveData();
    }

    // UI에서 구매 버튼 클릭 시 호출
    public bool TryUnlockStage(StageDefinitionSO stage)
    {
        if (!PhotonNetwork.InRoom) throw new Exception();

        if (_roomWallet.IsStageUnlocked(stage.StageId)
            || _roomWallet.TotalStars < stage.RequiredStars
            || _roomWallet.Coin.Value < stage.UnlockPrice)
        {
            Debug.Log($"[RoomDataManager] {stage.name} Stage 해금에 실패하였습니다. 해금 조건을 확인하세요.");
            return false;
        }



        photonView.RPC(nameof(RPC_OnStageUnlocked), RpcTarget.All, stage.StageId, stage.UnlockPrice);
        SaveData();
        return true;
    }

    [PunRPC]
    private void RPC_OnStageUnlocked(string stageId, int unlockPrice)
    {
        _roomWallet = _roomWallet
            .SpendCoin(unlockPrice)
            .UnlockStage(stageId);

        Debug.Log($"[RoomDataManager] {stageId} Stage가 해금되었습니다.");
        OnRoomDataChanged?.Invoke(Coin.Value, Star);
    }

    public async UniTask<bool> IsRoomDataExist(string roomCode)
    {
        if (_roomDataRepository == null) return false;

        bool isExist = await _roomDataRepository.IsExist(roomCode);
        return isExist;
    }
    
    public override void OnJoinedRoom()
    {
        LoadRoomData();
    }

    public void LoadRoomData()
    {
        LoadRoomDataAsync().Forget();
    }

    public override void OnLeftRoom()
    {
        _currentRoomCode = null;
        _roomWallet = null;
    }

    private async UniTask LoadRoomDataAsync()
    {
        await LoadCurrentRoom(PhotonNetwork.CurrentRoom.Name);
        OnRoomDataChanged?.Invoke(Coin.Value, Star);
    }
}
