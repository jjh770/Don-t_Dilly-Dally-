using UnityEngine;

public class StageBootstrapper : MonoBehaviour
{
    [SerializeField] private RewardView _rewardView;

    private RewardPresenter _rewardPresenter;
    void Start()
    {
        _rewardPresenter = new RewardPresenter(_rewardView);
        _rewardView.SetPresenter(_rewardPresenter);
    }
}
