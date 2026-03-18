using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public interface IInputProvider
    {
        bool GetKeyDown(KeyCode key);
    }
}
