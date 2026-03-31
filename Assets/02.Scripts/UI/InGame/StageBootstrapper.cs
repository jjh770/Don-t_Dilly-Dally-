using UnityEngine;

public class StageBootstrapper : MonoBehaviour
{
    [SerializeField] private RewardView _rewardView;

    private RewardPresenter _rewardPresenter;
    private void Start()
    {
        _rewardPresenter = new RewardPresenter(_rewardView);
        _rewardView.SetPresenter(_rewardPresenter);
    }

    private void OnDestroy()
    {
        _rewardPresenter.Dispose();
    }
}
