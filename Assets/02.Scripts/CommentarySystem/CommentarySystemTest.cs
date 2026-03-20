using UnityEngine;

public class CommentarySystemTest : MonoBehaviour
{
    [SerializeField] private CommentaryNetworkBridge _networkBridge;

    private void Update()
    {
        if (_networkBridge == null) return;

        // 테스트 키 입력 - RequestEvent로 Host에게 전달
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _networkBridge.RequestEvent(EventType.GameStart, "게임이 시작되었습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _networkBridge.RequestEvent(EventType.PatientCritical, "환자의 상태가 급격히 악화되고 있습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _networkBridge.RequestEvent(EventType.AssistDeliverItem, "어시스트가 제세동기를 전달했습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            _networkBridge.RequestEvent(EventType.MachineBroken, "장비가 고장났습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            _networkBridge.RequestEvent(EventType.SurgerySuccess, "수술이 성공했습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _networkBridge.RequestEvent(EventType.SurgeryFail, "수술이 실패했습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            _networkBridge.RequestEvent(EventType.TeamCooperation, "수술팀이 완벽하게 협동하고 있습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            _networkBridge.RequestEvent(EventType.PlayerMistake, "플레이어가 실수로 장비를 떨어뜨렸습니다.");
        }
    }
}
