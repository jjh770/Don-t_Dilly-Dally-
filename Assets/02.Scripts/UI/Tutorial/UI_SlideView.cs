using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SlideView : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Image _slideImage;

    [Header("Text")]
    [SerializeField] private TMP_Text _stepLabel;
    [SerializeField] private TMP_Text _titleText;

    public bool IsReady { get; private set; } = false;

    public void Setup(UI_SlideData data)
    {
        _stepLabel.text = data.StepLabel;
        _titleText.text = data.Title;
        _slideImage.sprite = data.Image;

        IsReady = true;
    }

    private void Reset()
    {
        _stepLabel.text = string.Empty;
        _titleText.text = string.Empty;
        _slideImage.sprite = null;
        IsReady = false;
    }
}
