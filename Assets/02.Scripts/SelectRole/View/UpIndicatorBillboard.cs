using System.Collections;
using UnityEngine;

public class UpIndicatorBillboard : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform[] _children;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;

        if (_player == null)
        {
            _player = transform.root;
        }

        StartCoroutine(StartBillboard());
    }

    private IEnumerator StartBillboard()
    {
        while (true)
        {
            // 부모는 카메라를 바라봄
            transform.LookAt(_mainCamera.transform);
            transform.Rotate(0f, 180f, 0f);

            // 자식들은 Y축만 플레이어를 따라감
            if (_player != null)
            {
                float playerY = _player.eulerAngles.y;
                foreach (var child in _children)
                {
                    if (child != null)
                    {
                        child.rotation = Quaternion.Euler(0f, playerY, 0f);
                    }
                }
            }

            yield return null;
        }
    }
}