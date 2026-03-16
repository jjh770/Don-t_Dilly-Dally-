using UnityEngine;

public interface IHoldable : IInteractable
{
    void Hold(Transform holdPoint);
    void Throw(Vector3 direction, float force);
    void Drop();
}