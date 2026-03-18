using System.Collections;
using UnityEngine;

public class Billboard : MonoBehaviour
{
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
            transform.LookAt(_mainCamera.transform);
            transform.Rotate(0, 180f, 0);
            yield return null;
        }
    }
}
