using UnityEngine;

// 바퀴를 지정된 축으로 계속 회전시키는 스크립트.
public class WheelSpin : MonoBehaviour
{
    [SerializeField] private float _spinSpeed = 500f;

    private void Update()
    {
        // X축 기준으로 회전 (바퀴가 앞으로 굴러가는 방향).
        transform.Rotate(_spinSpeed * Time.deltaTime, 0f, 0f);
    }
}