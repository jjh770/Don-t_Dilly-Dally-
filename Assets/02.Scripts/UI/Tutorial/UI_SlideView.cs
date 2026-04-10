using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SlideView : MonoBehaviour
{
    [Header("Layout Groups")]
    [SerializeField] private GameObject _imagePanel;
    [SerializeField] private GameObject _textPanel;
    [SerializeField] private HorizontalLayoutGroup _horizontalLayout;

    [Header("Image")]
    [SerializeField] private Image _slideImage;

    [Header("Text")]
    [SerializeField] private TMP_Text _stepLabel;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descText;

    public bool IsReady { get; private set; } = false;

    public void Setup(UI_SlideData data)
    {
        _stepLabel.text = data.stepLabel;
        _titleText.text = data.title;
        _descText.text = data.description;

        ApplyLayout(data.layout, data.image);

        IsReady = true;
    }

    void ApplyLayout(UI_SlideData.Layout layout, Sprite image)
    {
        bool hasImage = layout != UI_SlideData.Layout.TextOnly;

        _imagePanel.SetActive(hasImage);

        if (hasImage)
        {
            _slideImage.sprite = image;

            // 이미지 패널 좌우 위치 전환
            _imagePanel.transform.SetSiblingIndex(
                layout == UI_SlideData.Layout.ImageLeft ? 0 : 1
            );
        }
    }

    public void Reset()
    {
        _stepLabel.text = string.Empty;
        _titleText.text = string.Empty;
        _descText.text = string.Empty;
        _slideImage.sprite = null;
        IsReady = false;
    }
}
