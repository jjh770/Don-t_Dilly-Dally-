using UnityEngine;

namespace DontDillyDally.Data
{
    public class NetworkItemState : MonoBehaviour
    {
        [SerializeField] private bool _hasLeftSource;

        public bool HasLeftSource => _hasLeftSource;

        public void MarkLeftSource()
        {
            _hasLeftSource = true;
        }

        public void ResetSourceState()
        {
            _hasLeftSource = false;
        }
    }
}
