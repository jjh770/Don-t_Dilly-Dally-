using DontDillyDally.Data;
using UnityEngine;

public class PlayerInteractableOutlineHandler : MonoBehaviour
{
    [Header("아웃라인 설정")]
    [SerializeField] private Color _defaultOutlineColor = new Color(1f, 1f, 0.6f, 1f);  // bright warm yellow
    [SerializeField] private Color _compatibleOutlineColor = new Color(0.2f, 1f, 0.3f, 1f);  // vivid green
    [SerializeField] private Color _incompatibleOutlineColor = new Color(1f, 0.2f, 0.2f, 1f);  // vivid red
    [SerializeField] private float _outlineWidth = 5f;
    [SerializeField] private Outline.Mode _outlineMode = Outline.Mode.OutlineVisible;

    private PlayerInteractionAbility _interactionAbility;
    private IInteractable _currentOutlinedInteractable;

    private void Awake()
    {
        _interactionAbility = GetComponent<PlayerInteractionAbility>();
    }

    private void OnEnable()
    {
        _interactionAbility.OnNearestInteractableChanged += HandleNearestInteractableChanged;
        _interactionAbility.OnHeldItemChanged += HandleHeldItemChanged;
    }

    private void OnDisable()
    {
        _interactionAbility.OnNearestInteractableChanged -= HandleNearestInteractableChanged;
        _interactionAbility.OnHeldItemChanged -= HandleHeldItemChanged;
    }

    private void HandleNearestInteractableChanged(IInteractable previous, IInteractable current)
    {
        SetOutlineEnabled(previous, false);
        _currentOutlinedInteractable = current;
        RefreshOutline();
    }

    private void HandleHeldItemChanged(ItemObject heldItem)
    {
        RefreshOutline();
    }

    private void RefreshOutline()
    {
        if (_currentOutlinedInteractable == null)
            return;

        Color color = ResolveOutlineColor(_currentOutlinedInteractable);
        SetOutlineEnabled(_currentOutlinedInteractable, true, color);
    }

    private Color ResolveOutlineColor(IInteractable interactable)
    {
        ItemObject heldItem = _interactionAbility.CurrentHeldItem;
        if (heldItem == null)
            return _defaultOutlineColor;

        if (interactable is IItemAcceptor acceptor)
            return acceptor.CanAcceptItem(heldItem) ? _compatibleOutlineColor : _incompatibleOutlineColor;

        return _defaultOutlineColor;
    }

    private void SetOutlineEnabled(IInteractable interactable, bool enabled, Color? color = null)
    {
        if (interactable is not Component component)
            return;

        if (!component.TryGetComponent(out Outline outline))
        {
            if (!enabled)
                return;

            outline = component.gameObject.AddComponent<Outline>();
        }

        outline.OutlineMode = _outlineMode;
        outline.OutlineColor = color ?? _defaultOutlineColor;
        outline.OutlineWidth = _outlineWidth;
        outline.enabled = enabled;
    }
}
