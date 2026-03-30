using System.Collections.Generic;
using System.Linq;

public class RoomWallet
{
    private readonly RoomCurrency _money;
    private readonly Dictionary<string, StageStars> _stagesStars;

    public RoomCurrency Money => _money;
    public Dictionary<string, StageStars> StagesStars => _stagesStars;

    public RoomWallet(RoomCurrency money, Dictionary<string, StageStars> stageStars)
    {
        _money = money;
        _stagesStars = stageStars;
    }

    public static RoomWallet Default =>
        new(RoomCurrency.Default(ERoomCurrencyType.Money), new Dictionary<string, StageStars>());

    // ── 별 조회 ────────────────────────────────────────────────────────────
    public StageStars GetStageStars(string stageId) =>
        _stagesStars.TryGetValue(stageId, out var s) ? s : StageStars.Default;

    public int TotalStars => _stagesStars.Values.Sum(s => s.Best);

    // ── 해금 여부 (키 존재 여부로 판단) ───────────────────────────────────
    public bool IsStageUnlocked(string stageId) => _stagesStars.ContainsKey(stageId);

    // ── 스테이지 해금 (딕셔너리에 추가) ───────────────────────────────────
    public RoomWallet UnlockStage(string stageId)
    {
        if (IsStageUnlocked(stageId)) return this;

        var next = new Dictionary<string, StageStars>(_stagesStars);
        next[stageId] = StageStars.Default;
        return new(_money, next);
    }

    // ── 돈: 매 클리어 누적 ────────────────────────────────────────────────
    public RoomWallet AddMoney(int amount) =>
        new(_money.Add(amount), _stagesStars);

    // ── 별: 스테이지별 최고 기록만 유지 ───────────────────────────────────
    public RoomWallet UpdateStars(string stageId, int newStars)
    {
        StageStars updated = GetStageStars(stageId).KeepBest(newStars);
        if (updated.Best == GetStageStars(stageId).Best) return this;

        var next = new Dictionary<string, StageStars>(_stagesStars);
        next[stageId] = updated;
        return new(_money, next);
    }

    // ── 보상 한 번에 적용 ─────────────────────────────────────────────────
    public RoomWallet ApplyReward(string stageId, StageReward reward) =>
        AddMoney(reward.Money).UpdateStars(stageId, reward.Stars);
}
