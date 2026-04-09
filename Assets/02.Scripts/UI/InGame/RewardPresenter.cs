using DontDillyDally.StageFlow;
using UnityEngine;

public class RewardPresenter
{
    private readonly RewardView _view;
    private readonly StageFlowManager _stageFlowManager;

    public RewardPresenter(RewardView view)
    {
        _view = view;
        _stageFlowManager = StageFlowManager.Instance;

        SetupView();

        if (_stageFlowManager != null)
        {
            _stageFlowManager.OnStageRewardGranted += HandleStageRewardGranted;
        }
    }

    public void SetupView()
    {
        _view.InitializeReward(RoomDataManager.Instance.Coin.Value, RoomDataManager.Instance.Star);
    }

    public void ReturnWaitingRoom()
    {
        PhotonServerManager.Instance.ReturnWaitingRoom();
    }

    private void HandleStageRewardGranted(StageReward reward, StageResult result)
    {
        _view.Show(() =>
        {
            _view.PlayRewardSequence(reward.Stars, result.SurvivalRatio, RoomDataManager.Instance.Coin.Value, RoomDataManager.Instance.Star, reward.SummaryText, reward.Money - reward.MoneyDelta, reward.MoneyDelta);
        });
    }

    public void Dispose()
    {
        if (_stageFlowManager != null)
        {
            _stageFlowManager.OnStageRewardGranted -= HandleStageRewardGranted;
        }
    }
}
