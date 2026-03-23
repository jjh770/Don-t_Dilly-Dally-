using UnityEngine;
using UnityEngine.UI;

public class WaitingRoom : MonoBehaviour
{
    [SerializeField] private GameObject _customizingUI;
    [SerializeField] private Button _button;


    private void OnEnable()
    {
        _button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        if (_customizingUI != null)
        {
            _customizingUI.SetActive(true);
        }
    }
}
