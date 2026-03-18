using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class UnityInputProvider : IInputProvider
    {
        public bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);
    }
}
