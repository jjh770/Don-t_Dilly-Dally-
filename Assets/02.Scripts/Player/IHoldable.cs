using UnityEngine;

public interface IHoldable : IInteractable
{
    new Transform Transform { get; }
    void Interact(Transform holdPoint, int holderActorNumber);
    void Hold(Transform holdPoint);
    void Throw(Vector3 direction, float force, Collider[] throwerColliders = null);
}