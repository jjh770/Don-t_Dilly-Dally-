using System.Collections;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private bool _lockYAxisOnly;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        StartCoroutine(StartBillboard());
    }

    private IEnumerator StartBillboard()
    {
        while (true)
        {
            if (_mainCamera != null)
            {
                Vector3 target = _mainCamera.transform.position;
                if (_lockYAxisOnly)
                {
                    target.y = transform.position.y;
                }

                transform.LookAt(target);
                transform.Rotate(0, 180f, 0);
            }

            yield return null;
        }
    }
}
