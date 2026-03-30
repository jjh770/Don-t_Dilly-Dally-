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
    [SerializeField] private TextMeshProUGUI _errorMessageText;

    [SerializeField] private float _errorFadeDuration = 0.25f;

    [SerializeField] private float _errorVisibleDuration = 1.5f;

    public Button AttendancePopupButton => _attendancePopupButton;


    private Tween _errorTween;

    private RoomPresenter _presenter;

    private void Start()
    {
        SetErrorAlpha(0f);
    }

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
        if (_errorMessageText == null) return;

        _errorTween?.Kill();
        _errorMessageText.text = message;
        SetErrorAlpha(0f);

        _errorTween = DOTween.Sequence()
            .Append(_errorMessageText.DOFade(1f, _errorFadeDuration))
            .AppendInterval(_errorVisibleDuration)
            .Append(_errorMessageText.DOFade(0f, _errorFadeDuration));
    }

    private void SetErrorAlpha(float alpha)
    {
        if (_errorMessageText == null) return;

        Color color = _errorMessageText.color;
        color.a = alpha;
        _errorMessageText.color = color;
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
