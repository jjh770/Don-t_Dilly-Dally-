using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DayListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Image _completeImage;

    public void SetItem(int day, Sprite itemSprite, bool isComplete)
    {
        ResetComplete();
        _dayText.text = $"{day}";
        _itemImage.sprite = itemSprite;

        if (isComplete)
        {
            ShowComplete();
        }
    }

    public void ShowComplete()
    {
        _completeImage.enabled = true;
    }

    public void SetComplete()
    {
        _completeImage.enabled = true;

        var t = _completeImage.transform;
        t.DOKill();

        t.localScale = Vector3.zero;

        t.DOScale(1.2f, 0.15f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                t.DOScale(1f, 0.1f).SetEase(Ease.InQuad);
            });
    }

    public void ResetComplete()
    {
        _completeImage.enabled = false;
    }
}
