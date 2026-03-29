using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class AttendanceView : UIPopupBase
{
    [SerializeField] private UI_DayListItem[] _dayListItems;

    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private RectTransform _listRectTransform;
    [SerializeField] private Button _closeButton;

    private AttendancePresenter _presenter;

    public void Start()
    {
        foreach (var dayItem in _dayListItems)
        {
            dayItem.gameObject.SetActive(false);
        }
    }

    public void OnEnable()
    {
        _closeButton.onClick.AddListener(PopupClose);
    }

    private void PopupClose()
    {
        Hide();
        _presenter.OnPopupClose();
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
            item.Initialize(i, null, isComplete);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_listRectTransform);
    }
    public void SetComplete(int totalDays)
    {
        var index = totalDays - 1;
        _dayListItems[index].PlayCompleteAnimation();
    }

    protected override void OnShow()
    {
        _presenter.OnPopupShow();
    }

    public void SetName(string name)
    {
        _nameText.text = name;
    }

    public void OnDisable()
    {
        _closeButton.onClick.RemoveListener(PopupClose);
    }
}
