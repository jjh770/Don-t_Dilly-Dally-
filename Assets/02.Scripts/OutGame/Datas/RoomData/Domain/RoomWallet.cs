using System.Collections.Generic;
using System.Linq;
using DontDillyDally.StageFlow;

public class RoomWallet
{
    private readonly RoomCurrency _coin;
    private readonly Dictionary<string, StageStars> _stagesStars;
    private readonly HospitalLevel _hospitalLevel;
    public RoomCurrency Coin => _coin;
    public Dictionary<string, StageStars> StagesStars => _stagesStars;

    public HospitalLevel HospitalLevel => _hospitalLevel;
    public RoomWallet(RoomCurrency coin, Dictionary<string, StageStars> stageStars, HospitalLevel hospitalLevel)
    {
        _coin = coin;
        _stagesStars = stageStars;
        _hospitalLevel = hospitalLevel;
    }

    public static RoomWallet Default =>
        new(RoomCurrency.Default(ERoomCurrencyType.Coin), new Dictionary<string, StageStars>(), HospitalLevel.Default);

    // ── 별 조회 ────────────────────────────────────────────────────────────
    public StageStars GetStageStars(string stageId) =>
        _stagesStars.TryGetValue(stageId, out var s) ? s : StageStars.Default;

    public int TotalStars => _stagesStars.Values.Sum(s => s.Best);


    // ── 병원 업그레이드  ───────────────────────────────────

    public bool IsStageAvailable(StageDefinitionSO stage)
        => _hospitalLevel.Value >= stage.RequiredHospitalLevel;

    public RoomWallet UpgradeHospital(int upgradeCost)
        => new(_coin.Minus(upgradeCost), _stagesStars, _hospitalLevel.Upgrade());


    // ── 돈: 매 클리어 누적 ────────────────────────────────────────────────
    public RoomWallet AddMoney(int amount) =>
        new(_coin.Add(amount), _stagesStars, _hospitalLevel);


    // ── 돈: 소비 ────────────────────────────────────────────────
    public RoomWallet SpendCoin(int amount) =>
    new(_coin.Minus(amount), _stagesStars, _hospitalLevel);

    // ── 별: 스테이지별 최고 기록만 유지 ───────────────────────────────────
    public RoomWallet UpdateStars(string stageId, int newStars)
    {
        StageStars updated = GetStageStars(stageId).KeepBest(newStars);
        if (updated.Best == GetStageStars(stageId).Best) return this;

        var next = new Dictionary<string, StageStars>(_stagesStars);
        next[stageId] = updated;
        return new(_coin, next, _hospitalLevel);
    }

    // ── 보상 한 번에 적용 ─────────────────────────────────────────────────
    public RoomWallet ApplyReward(string stageId, StageReward reward) =>
        AddMoney(reward.Money).UpdateStars(stageId, reward.Stars);


}
