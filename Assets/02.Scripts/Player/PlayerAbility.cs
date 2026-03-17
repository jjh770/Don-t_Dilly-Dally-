using UnityEngine;

public class PlayerAbility : MonoBehaviour
{
    protected PlayerController _owner { get; private set; }

    protected virtual void Awake()
    {
        _owner = GetComponent<PlayerController>();
    }
}
