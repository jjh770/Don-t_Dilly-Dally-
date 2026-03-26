using UnityEngine;

public class CharacterPreviewCameraForLobby : MonoBehaviour
{
    [Header("Follow 세팅")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, 7.1f);

    [Header("Look 세팅")]
    [SerializeField] private Vector3 _lookOffset = new Vector3(0f, 0.8f, 0f);

    public void SetTransform(Transform target)
    {
        transform.position = target.position + target.rotation * _offset;
        transform.LookAt(target.position + _lookOffset);
    }
}
