using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingView : UIPopupBase
{
    [Header("Sound")]
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private TextMeshProUGUI _bgmValueText;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private TextMeshProUGUI _sfxValueText;

    [Header("Buttons")]
    [SerializeField] private Button _quitGameButton;
    [SerializeField] private Button _closeButton;

    private SettingPresenter _presenter;

    private void OnEnable()
    {
        if (_bgmSlider != null)
        {
            _bgmSlider.onValueChanged.AddListener(HandleBgmSliderChanged);
            _bgmSlider.minValue = 0f;
            _bgmSlider.maxValue = 1f;
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
            _sfxSlider.minValue = 0f;
            _sfxSlider.maxValue = 1f;
        }

        if (_quitGameButton != null)
        {
            _quitGameButton.onClick.AddListener(HandleQuitButtonClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
        } 
    }

    private void OnDisable()
    {
        if (_bgmSlider != null)
        {
            _bgmSlider.onValueChanged.RemoveListener(HandleBgmSliderChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
        }

        if (_quitGameButton != null)
        {
            _quitGameButton.onClick.RemoveListener(HandleQuitButtonClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Hide);
        }
    }

    public void Initialize(SettingPresenter presenter)
    {
        _presenter = presenter;
    }

    public void SetBgmVolume(float value)
    {
        SetSliderValue(_bgmSlider, value);
        SetValueText(_bgmValueText, value);
    }

    public void SetSfxVolume(float value)
    {
        SetSliderValue(_sfxSlider, value);
        SetValueText(_sfxValueText, value);
    }

    protected override void OnShow()
    {
        _presenter?.RefreshView();
    }

    private void HandleBgmSliderChanged(float value)
    {
        SetValueText(_bgmValueText, value);
        _presenter?.HandleBgmVolumeChanged(value);
    }

    private void HandleSfxSliderChanged(float value)
    {
        SetValueText(_sfxValueText, value);
        _presenter?.HandleSfxVolumeChanged(value);
    }

    private void HandleQuitButtonClicked()
    {
        _presenter?.HandleQuitButtonClicked();
    }

    private static void SetSliderValue(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    private static void SetValueText(TextMeshProUGUI valueText, float value)
    {
        if (valueText == null)
        {
            return;
        }

        valueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }
}
