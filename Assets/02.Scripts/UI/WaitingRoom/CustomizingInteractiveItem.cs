using UnityEngine;

public class CustomizingInteractiveItem : MonoBehaviour, IInteractable
{
    private bool _isInteracting;
    [SerializeField] private UI_Customizing _customizingUI;

    private CustomizingUIViewModel _viewModel;

    public bool IsInteracting => _isInteracting;
    public Transform Transform => transform;

    public void Interact(Transform interactor)
    {
        _isInteracting = true;

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

    public void StopInteract()
    {
        _isInteracting = false;
    }
}
