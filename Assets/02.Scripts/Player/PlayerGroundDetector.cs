using UnityEngine;

public class PlayerGroundDetector : MonoBehaviour
{
    [SerializeField] private LayerMask _iceLayerMask;

    private int _iceZoneCount;

    public bool IsOnIce => _iceZoneCount > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (IsInIceLayer(other.gameObject))
        {
            _iceZoneCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsInIceLayer(other.gameObject))
        {
            _iceZoneCount--;
            if (_iceZoneCount < 0) _iceZoneCount = 0;
        }
    }

    private bool IsInIceLayer(GameObject obj)
    {
        return (_iceLayerMask & (1 << obj.layer)) != 0;
    }
}
