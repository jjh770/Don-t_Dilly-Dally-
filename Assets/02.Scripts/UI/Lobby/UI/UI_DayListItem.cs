using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DayListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Image _completeImage;
    [SerializeField] private float _animationDelay = 0.5f;

    public void Initialize(int day, Sprite itemSprite, bool isComplete)
    {
        ClearComplete();
        _dayText.text = $"{day}";
        _itemImage.sprite = itemSprite;

        if (isComplete)
        {
            MarkComplete();
        }
    }

    public void MarkComplete()
    {
        _completeImage.enabled = true;
    }

    public void PlayCompleteAnimation()
    {
       
        var t = _completeImage.transform;
        t.DOKill();

        t.localScale = Vector3.one * 5f;

        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(_animationDelay)
             .AppendCallback(() => _completeImage.enabled = true)

            .Append(t.DOScale(0.8f, 0.08f)
            .SetEase(Ease.InQuad))
            .AppendCallback(() => SoundManager.Instance.Play(SFXKey.StampSound, SoundType.Local))
           .Append(t.DOScale(1.15f, 0.12f)
            .SetEase(Ease.OutBack))       

           .Append(t.DOScale(1f, 0.08f)
            .SetEase(Ease.OutQuad));       
    }

    public void ClearComplete()
    {
        _completeImage.enabled = false;
    }
}
