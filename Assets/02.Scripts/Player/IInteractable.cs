using UnityEngine;

public interface IInteractable
{
    bool IsInteracting { get; }
    Transform Transform { get; }
    void Interact();
    void StopInteract();
}
