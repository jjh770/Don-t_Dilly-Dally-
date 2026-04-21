using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HospitalUpgradeView : MonoBehaviour
{
    [SerializeField] private GameObject _upgradeButtonRoot;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private GameObject _maxLevelTextObject;
    [SerializeField] private GameObject _requirementStampImageObject;
    [SerializeField] private GameObject _coinRequirementRoot;
    [SerializeField] private TMP_Text _coinRequirementText;
    [SerializeField] private Slider _coinSlider;
    [SerializeField] private GameObject _starRequirementRoot;
    [SerializeField] private TMP_Text _starRequirementText;
    [SerializeField] private Slider _starSlider;
    [SerializeField] private Color _defaultSliderFillColor = Color.white;
    [SerializeField] private Color _completedSliderFillColor = Color.green;

    private UI_HospitalUpgradePresenter _presenter;

    private void OnEnable()
    {
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(HandleUpgradeButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveListener(HandleUpgradeButtonClicked);
        }
    }

    public void Initialize(UI_HospitalUpgradePresenter presenter)
    {
        _presenter = presenter;
    }

    public void Render(int currentCoin, int requiredCoin, int currentStar, int requiredStar, bool canUpgrade, bool hasNextLevel, bool isMaster)
    {
        bool isCoinRequirementMet = SetRequirementSection(_coinRequirementRoot, _coinRequirementText, _coinSlider, currentCoin, requiredCoin);
        bool isStarRequirementMet = SetRequirementSection(_starRequirementRoot, _starRequirementText, _starSlider, currentStar, requiredStar);
        bool hasCoinRequirement = requiredCoin > 0;
        bool hasStarRequirement = requiredStar > 0;
        bool hasAnyRequirement = hasCoinRequirement || hasStarRequirement;
        bool areAllVisibleRequirementsMet =
            (!hasCoinRequirement || isCoinRequirementMet) &&
            (!hasStarRequirement || isStarRequirementMet);

        if (_maxLevelTextObject != null)
        {
            _maxLevelTextObject.SetActive(!hasNextLevel);
        }

        if (_requirementStampImageObject != null)
        {
            _requirementStampImageObject.SetActive(hasNextLevel && hasAnyRequirement && areAllVisibleRequirementsMet);
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = hasNextLevel && canUpgrade;
            _upgradeButtonRoot.gameObject.SetActive(hasNextLevel && isMaster);
        }
    }

    private bool SetRequirementSection(GameObject root, TMP_Text requirementText, Slider slider, int currentValue, int requiredValue)
    {
        bool shouldShow = requiredValue > 0;

        if (root != null)
        {
            root.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            SetSliderFillColor(slider, _defaultSliderFillColor);
            return false;
        }

        if (requirementText != null)
        {
            requirementText.text = $"{requiredValue}";
        }

        bool isRequirementMet = currentValue >= requiredValue;
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01((float)currentValue / requiredValue));
            SetSliderFillColor(slider, isRequirementMet ? _completedSliderFillColor : _defaultSliderFillColor);
        }

        return isRequirementMet;
    }

    private void SetSliderFillColor(Slider slider, Color color)
    {
        if (slider?.fillRect == null)
        {
            return;
        }

        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }

    private void HandleUpgradeButtonClicked()
    {
        _presenter?.TryUpgradeHospital();
    }
}
