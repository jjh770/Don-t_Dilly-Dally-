using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomView : MonoBehaviour
{
    [SerializeField] private Button enterHospitalButton;
    [SerializeField] private Button createHospitalButton;
    [SerializeField] private TMP_InputField roomCodeInputField;

    private RoomPresenter _presenter;

    private void OnEnable ()
    {
        enterHospitalButton.onClick.AddListener(OnEnterButtonClick);
        createHospitalButton.onClick.AddListener(OnCreateButtonClick);
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

    private void OnDisable()
    {
        enterHospitalButton.onClick.RemoveAllListeners();
        createHospitalButton.onClick.RemoveAllListeners();
    }
}
