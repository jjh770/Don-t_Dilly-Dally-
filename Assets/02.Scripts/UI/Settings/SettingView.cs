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
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
        }

        if (_quitGameButton != null)
        {
            _quitGameButton.onClick.AddListener(HandleQuitButtonClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(HandleCloseButtonClicked);
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
            _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
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

    protected override void HandleCloseHotkey()
    {
        base.HandleCloseHotkey();
        _presenter?.HandleCloseRequested();
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

    private void HandleCloseButtonClicked()
    {
        Hide();
        _presenter?.HandleCloseRequested();
    }

    private static void SetSliderValue(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
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
