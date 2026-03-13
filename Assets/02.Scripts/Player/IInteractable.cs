using UnityEngine;

public interface IHoldable
{
    bool IsHeld { get; }
    void Hold(Transform holdPoint);
    void Throw(Vector3 direction, float force);
    void Drop();
}

public interface IPushable
{
    void Push(Vector3 direction, float force);
}
