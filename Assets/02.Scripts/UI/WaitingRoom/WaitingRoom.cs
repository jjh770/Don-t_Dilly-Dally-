using UnityEngine;
using UnityEngine.UI;

public class WaitingRoom : MonoBehaviour
{
    [SerializeField] private UI_Customizing _customizingUI;
    [SerializeField] private Button _button;

    private CustomizingUIViewModel _viewModel;

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
        if (_customizingUI == null) return;

        if (_viewModel == null)
        {
            var manager = CustomizingManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[WaitingRoom] CustomizingManager가 없습니다.");
                return;
            }
            _viewModel = new CustomizingUIViewModel(manager);
            _customizingUI.Initialize(_viewModel);
        }

        _customizingUI.Show();
    }
}
