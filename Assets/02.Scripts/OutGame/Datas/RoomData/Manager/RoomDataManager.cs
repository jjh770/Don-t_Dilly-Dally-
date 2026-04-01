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

    private IRoomCurrencyRepository _roomDataRepository;
    private RoomWallet _roomWallet;
    private string _currentRoomCode;

    private const byte STAGE_UNLOCK_EVENT = 100;

    public int Star => _roomWallet.TotalStars;
    public RoomCurrency Coin => _roomWallet.Coin;

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

    public async UniTask<bool> IsRoomDataExist(string roomCode)
    {
        if (_roomDataRepository == null) return false;
        return await _roomDataRepository.IsExist(roomCode);
    }

    // ── 스테이지 해금 ─────────────────────────────────────────────────────
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

    public bool TryUnlockStage(StageDefinitionSO stage)
    {
        if (!PhotonNetwork.InRoom) throw new Exception("병원 접속 상태가 아닙니다.");

        if (_roomWallet.IsStageUnlocked(stage.StageId)
            || _roomWallet.TotalStars < stage.RequiredStars
            || _roomWallet.Coin.Value < stage.UnlockPrice)
        {
            Debug.Log($"[RoomDataManager] {stage.name} Stage 해금에 실패하였습니다.");
            return false;
        }
        Unlock(stage.StageId, stage.UnlockPrice);
        SaveData();

        BroadcastUnlock(stage.StageId, stage.UnlockPrice);
        return true;
    }

    private void BroadcastUnlock(string stageId, int unlockPrice)
    {
        PhotonNetwork.RaiseEvent(
            STAGE_UNLOCK_EVENT,
            new object[] { stageId, unlockPrice },
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable
        );
    }

    private void OnEventReceived(EventData data)
    {
        if (data.Code != STAGE_UNLOCK_EVENT) return;
        if (data.CustomData is not object[] payload) return;

        string stageId = Convert.ToString(payload[0]);
        int unlockPrice = Convert.ToInt32(payload[1]);
        Unlock(stageId, unlockPrice);
    }

    private void Unlock(string stageId, int unlockPrice)
    {
        _roomWallet = _roomWallet
            .SpendCoin(unlockPrice)
            .UnlockStage(stageId);

        Debug.Log($"[RoomDataManager] {stageId} Stage가 해금되었습니다.");
        OnRoomDataChanged?.Invoke(Coin.Value, Star);
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
