using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomView : MonoBehaviour
{
    [SerializeField] private Button _enterHospitalButton;
    [SerializeField] private Button _createHospitalButton;
    [SerializeField] private TMP_Dropdown _myHospitalDropdown;
    private ScrollRect _myHospitalDropdownScrollRect;

    [SerializeField] private TMP_InputField _roomCodeInputField;
    [SerializeField] private TMP_InputField _nickNameInputField;
    [SerializeField] private TextMeshProUGUI _errorMessageText;

    private RoomPresenter _presenter;

    private void OnEnable ()
    {
        _enterHospitalButton.onClick.AddListener(OnEnterButtonClick);
        _createHospitalButton.onClick.AddListener(OnCreateButtonClick);
        _nickNameInputField.onDeselect.AddListener(OnNickNameInputDeselect);

        _myHospitalDropdown.onValueChanged.AddListener(OnMyHospitalSelected);
    }

    private void OnMyHospitalSelected(int index)
    {
        _presenter.SelectMyHospital(index);
    }

    private void OnNickNameInputDeselect(string name)
    {
        _presenter.SetNickName(name);
    }

    public void Init(RoomPresenter presenter)
    {
        _presenter = presenter;
    }

    public void OnEnterButtonClick()
    {
        _presenter.EnterRoom(_roomCodeInputField.text);
    }

    public void OnCreateButtonClick()
    {
        _presenter.CreateRoom();
    }

    public void ShowErrorMessage(string message)
    {
        if (_errorMessageText != null)
            _errorMessageText.text = message;
    }

    public string GetCodeOfDropdown(int index)
    {
        return _myHospitalDropdown.options[index].text;
    }

    public void SetCodeInputField(string code)
    {
        _roomCodeInputField.text = code;
    }
    public void SetDropdown(string[] hospitals)
    {
        List<string> options = new List<string>();
        options.Add("선택하세요");
        options.AddRange(hospitals);

        _myHospitalDropdown.ClearOptions();
        _myHospitalDropdown.AddOptions(options);
        _myHospitalDropdown.value = 0;
        _myHospitalDropdown.RefreshShownValue();
    }

    private void OnDisable()
    {
        _enterHospitalButton.onClick.RemoveListener(OnEnterButtonClick);
        _createHospitalButton.onClick.RemoveListener(OnCreateButtonClick);
        _nickNameInputField.onDeselect.RemoveListener(OnNickNameInputDeselect);
        _presenter.Dispose();
    }
}
