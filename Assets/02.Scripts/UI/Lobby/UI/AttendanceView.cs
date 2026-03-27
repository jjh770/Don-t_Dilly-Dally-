using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class AttendanceView : MonoBehaviour
{
    [SerializeField] private UI_DayListItem[] _dayListItems;

    [SerializeField] private RectTransform _listRectTransform;

    private AttendancePresenter _presenter;

    public void Start()
    {
        foreach (var dayItem in _dayListItems)
        {
            dayItem.gameObject.SetActive(false);
        }
    }

    public void Init(AttendancePresenter presenter)
    {
        _presenter = presenter;
    }

    public void SetDayList(int totalDay, IRewardRepository rewardRepository)
    {
        int totalRewardCount = rewardRepository.GetRewardCount();

        for (int i = 1; i <= totalRewardCount; i++)
        {
            var item = _dayListItems[i-1];
            bool isComplete = totalDay >= i;

            item.gameObject.SetActive(true);   
            item.SetItem(i, null, isComplete);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_listRectTransform);
    }
}
