using UnityEngine;

// IInteractable이 자신이 아닌 다른 GameObject에 아웃라인을 그리도록 지정하고 싶을 때 구현한다.
// 예: 상호작용 감지용 빈 자식 오브젝트에 IInteractable을 두고, 실제 메쉬가 있는 부모를 아웃라인 타겟으로 지정.
public interface IOutlineTargetProvider
{
    GameObject OutlineTarget { get; }
}
