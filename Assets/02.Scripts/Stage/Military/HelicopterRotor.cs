using UnityEngine;

public class HelicopterRotor : MonoBehaviour
{
    [SerializeField] private Transform _mainRotor;
    [SerializeField] private Transform _tailRotor;
    [SerializeField] private float _mainRotorSpeed = 1200f;
    [SerializeField] private float _tailRotorSpeed = 1800f;

    private void Update()
    {
        if (_mainRotor != null)
        {
            _mainRotor.Rotate(Vector3.right * _mainRotorSpeed * Time.deltaTime);
        }
        if (_tailRotor != null)
        {
            _tailRotor.Rotate(Vector3.up * _tailRotorSpeed * Time.deltaTime);
        }
    }
}
