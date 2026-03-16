using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomView : MonoBehaviour
{
    [SerializeField] private Button enterHospitalButton;
    [SerializeField] private Button createHospitalButton;
    [SerializeField] private TMP_InputField roomCodeInputField;
    [SerializeField] private TMP_InputField nickNameInputField;
    [SerializeField] private TextMeshProUGUI errorMessageText;

    private RoomPresenter _presenter;

    private void OnEnable ()
    {
        enterHospitalButton.onClick.AddListener(OnEnterButtonClick);
        createHospitalButton.onClick.AddListener(OnCreateButtonClick);
        nickNameInputField.onDeselect.AddListener(OnNickNameInputDeselect);
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
        _presenter.EnterRoom(roomCodeInputField.text);
    }

    public void OnCreateButtonClick()
    {
        _presenter.CreateRoom();
    }

    public void ShowErrorMessage(string message)
    {
        if (errorMessageText != null)
            errorMessageText.text = message;
    }
    private void OnDisable()
    {
        enterHospitalButton.onClick.RemoveListener(OnEnterButtonClick);
        createHospitalButton.onClick.RemoveListener(OnCreateButtonClick);
        nickNameInputField.onDeselect.RemoveListener(OnNickNameInputDeselect);
        _presenter.Dispose();
    }
}
