using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HospitalUpgradeView : MonoBehaviour
{
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private GameObject _maxLevelTextObject;
    [SerializeField] private GameObject _coinRequirementRoot;
    [SerializeField] private TMP_Text _coinRequirementText;
    [SerializeField] private Slider _coinSlider;
    [SerializeField] private GameObject _starRequirementRoot;
    [SerializeField] private TMP_Text _starRequirementText;
    [SerializeField] private Slider _starSlider;

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
        SetRequirementSection(_coinRequirementRoot, _coinRequirementText, _coinSlider, currentCoin, requiredCoin);
        SetRequirementSection(_starRequirementRoot, _starRequirementText, _starSlider, currentStar, requiredStar);

        if (_maxLevelTextObject != null)
        {
            _maxLevelTextObject.SetActive(!hasNextLevel);
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = hasNextLevel && canUpgrade;
            _upgradeButton.gameObject.SetActive(hasNextLevel && isMaster);
        }
    }

    private void SetRequirementSection(GameObject root, TMP_Text requirementText, Slider slider, int currentValue, int requiredValue)
    {
        bool shouldShow = requiredValue > 0;

        if (root != null)
        {
            root.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        if (requirementText != null)
        {
            requirementText.text = $"{requiredValue}";
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01((float)currentValue / requiredValue));
        }
    }

    private void HandleUpgradeButtonClicked()
    {
        _presenter?.TryUpgradeHospital();
    }
}
