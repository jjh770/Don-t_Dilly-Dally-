using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomView : MonoBehaviour
{
    [SerializeField] private Button _enterHospitalButton;
    [SerializeField] private Button _createHospitalButton;
    [SerializeField] private Button _attendancePopupButton;

    [SerializeField] private UI_HospitalList _myHospitalList;

    [SerializeField] private TMP_InputField _roomCodeInputField;
    [SerializeField] private TMP_InputField _nickNameInputField;
    [SerializeField] private UI_Message _errorMessage;

    public Button AttendancePopupButton => _attendancePopupButton;


    private RoomPresenter _presenter;

    private void OnEnable ()
    {
        _enterHospitalButton.onClick.AddListener(OnEnterButtonClick);
        _createHospitalButton.onClick.AddListener(OnCreateButtonClick);
        _nickNameInputField.onDeselect.AddListener(OnNickNameInputDeselect);

        _myHospitalList.OnSelected += OnMyHospitalSelected;
        _myHospitalList.OnDeleteOption += OnMyHospitalDeleted;
    }

    private void OnMyHospitalSelected(string name)
    {
        _presenter.SelectMyHospital(name);
    }

    private void OnNickNameInputDeselect(string name)
    {
        _presenter.SetNickName(name);
    }

    public void Init(RoomPresenter presenter)
    {
        _presenter = presenter;
    }

    public void InitializeNicknameField(string nickName)
    {
        _nickNameInputField.text = nickName;
    }

    public void OnEnterButtonClick()
    {
        _presenter.EnterRoom(_roomCodeInputField.text);
    }

    public void OnCreateButtonClick()
    {
        _presenter.CreateRoom();
    }

    public void OnMyHospitalDeleted(string code)
    {
        _presenter.OnMyHospitalDeleted(code);
    }

    public void ShowErrorMessage(string message)
    {
        if (_errorMessage == null) return;

        _errorMessage.Show(message);
    }

    public void SetCodeInputField(string code)
    {
        _roomCodeInputField.text = code;
    }
    public void SetDropdown(IEnumerable<MyHospital> hospitals)
    {
        _myHospitalList.SetOptions(hospitals);
    }

    private void OnDisable()
    {
        _enterHospitalButton.onClick.RemoveListener(OnEnterButtonClick);
        _createHospitalButton.onClick.RemoveListener(OnCreateButtonClick);
        _nickNameInputField.onDeselect.RemoveListener(OnNickNameInputDeselect);
        _myHospitalList.OnSelected -= OnMyHospitalSelected;
        _myHospitalList.OnDeleteOption -= OnMyHospitalDeleted;
    }
}
