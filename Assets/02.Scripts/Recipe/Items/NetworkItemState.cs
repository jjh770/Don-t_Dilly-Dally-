using UnityEngine;

namespace DontDillyDally.Data
{
    public class NetworkItemState : MonoBehaviour
    {
        private const int InvalidActorNumber = -1;

        [SerializeField] private bool _hasLeftSource;
        [SerializeField] private bool _isHeld;
        [SerializeField] private int _holderActorNumber = InvalidActorNumber;

        public bool HasLeftSource => _hasLeftSource;
        public bool IsHeld => _isHeld;
        public int HolderActorNumber => _holderActorNumber;

        public void BeginHold(int holderActorNumber)
        {
            _isHeld = true;
            _holderActorNumber = holderActorNumber;
        }

        public void EndHold()
        {
            _isHeld = false;
            _holderActorNumber = InvalidActorNumber;
        }

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
