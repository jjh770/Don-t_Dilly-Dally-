using UnityEngine;

public class UIInteractiveItem : MonoBehaviour, IInteractable
{
    private bool _isInteracting;
    [SerializeField] private UIPopupBase _popUpUI;

    public bool IsInteracting => _isInteracting;
    public Transform Transform => transform;

    public void OnEnable()
    {
        if (_popUpUI != null)
        {
            _popUpUI.OnPopupClosed += HandlePopupClosed;
        }
    }
    public void Interact(Transform interactor)
    {
        _popUpUI.Show();
        _isInteracting = true;
    }

    public void StopInteract()
    {

    }

    public void HandlePopupClosed()
    {
        _isInteracting = false;
    }

    public void OnDisable()
    {
        if (_popUpUI != null)
        {
            _popUpUI.OnPopupClosed -= HandlePopupClosed;
        }
    }
}
