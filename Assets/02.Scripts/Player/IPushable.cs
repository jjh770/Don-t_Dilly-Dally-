using UnityEngine;

public interface IPushable : IInteractable
{
    void Push(Vector3 direction, float speed);
}
