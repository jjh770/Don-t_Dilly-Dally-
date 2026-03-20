using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomView : MonoBehaviour
{
    [SerializeField] private Button _enterHospitalButton;
    [SerializeField] private Button _createHospitalButton;
    [SerializeField] private Button _listOpenButton;

    [SerializeField] private UI_HospitalList _myHospitalList;

    [SerializeField] private TMP_InputField _roomCodeInputField;
    [SerializeField] private TMP_InputField _nickNameInputField;
    [SerializeField] private TextMeshProUGUI _errorMessageText;

    private RoomPresenter _presenter;

    private void OnEnable ()
    {
        _enterHospitalButton.onClick.AddListener(OnEnterButtonClick);
        _createHospitalButton.onClick.AddListener(OnCreateButtonClick);
        _nickNameInputField.onDeselect.AddListener(OnNickNameInputDeselect);
        _listOpenButton.onClick.AddListener(_myHospitalList.OpenToggle);

        _myHospitalList.OnSelected += OnMyHospitalSelected;
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

    public void ShowErrorMessage(string message)
    {
        if (_errorMessageText != null)
            _errorMessageText.text = message;
    }

    public void SetCodeInputField(string code)
    {
        _roomCodeInputField.text = code;
    }
    public void SetDropdown(string[] codes, string[] date)
    {
        _myHospitalList.SetOptions(codes, date);
    }

    private void OnDisable()
    {
        _enterHospitalButton.onClick.RemoveListener(OnEnterButtonClick);
        _createHospitalButton.onClick.RemoveListener(OnCreateButtonClick);
        _listOpenButton.onClick.RemoveListener(_myHospitalList.OpenToggle);
        _nickNameInputField.onDeselect.RemoveListener(OnNickNameInputDeselect);
        _myHospitalList.OnSelected -= OnMyHospitalSelected;

        _presenter.Dispose(); 
    }
}
