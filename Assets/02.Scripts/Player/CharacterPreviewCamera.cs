using UnityEngine;

public class CharacterPreviewCamera : MonoBehaviour
{
    public static CharacterPreviewCamera Instance { get; private set; }

    [Header("Follow 세팅")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, 7.1f);

    [Header("Look 세팅")]
    [SerializeField] private Vector3 _lookOffset = new Vector3(0f, 0.8f, 0f);

    private Transform _target;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        transform.position = _target.position + _target.rotation * _offset;
        transform.LookAt(_target.position + _lookOffset);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public static void SetLocalPlayerTarget(Transform target)
    {
        if (Instance != null)
        {
            Instance.SetTarget(target);
        }
    }
}