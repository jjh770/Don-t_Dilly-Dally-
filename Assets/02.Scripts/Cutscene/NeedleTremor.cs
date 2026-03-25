using UnityEngine;

// 계기판 바늘이 특정 각도 근처에서 부르르 떨리는 스크립트
public class NeedleTremor : MonoBehaviour
{
    [SerializeField] private float _baseAngle = -30f;
    [SerializeField] private float _trembleAmount = 3f;
    [SerializeField] private float _trembleSpeed = 25f;

    private void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * _trembleSpeed, 0f);
        float shake = (noise - 0.5f) * 2f * _trembleAmount;

        transform.localRotation = Quaternion.Euler(0f, 0f, _baseAngle + shake);
    }
}