using UnityEngine;

public interface IInteractable
{
    bool IsInteracting { get; }
    Transform Transform { get; }
    void Interact(Transform interactor);
    void StopInteract();
}
