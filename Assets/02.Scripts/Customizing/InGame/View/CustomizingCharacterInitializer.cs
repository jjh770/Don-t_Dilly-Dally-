using UnityEngine;

[RequireComponent(typeof(CustomizingCharacterController))]
public class CustomizingCharacterInitializer : MonoBehaviour
{
    private CustomizingCharacterViewModel _viewModel;

    private void Start()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[CustomizingCharacterInitializer] CustomizingManager가 없습니다.");
            return;
        }

        _viewModel = new CustomizingCharacterViewModel(manager);
        GetComponent<CustomizingCharacterController>().Initialize(_viewModel);
    }

    private void OnDestroy()
    {
        _viewModel?.Dispose();
        _viewModel = null;
    }
}
