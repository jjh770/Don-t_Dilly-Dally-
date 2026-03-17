using UnityEngine;

// 풀링 대상 프리팹에 부착하는 컴포넌트
// PunPoolManager가 풀 반납 시 이 컴포넌트의 PrefabId로 올바른 큐에 반환
public class PoolableObject : MonoBehaviour
{
    // 어느 풀로 반납해야 하는지 기억하는 ID (PunPoolManager가 자동 세팅)
    public string PrefabId { get; internal set; }
}
