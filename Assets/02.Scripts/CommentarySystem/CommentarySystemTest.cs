using UnityEngine;

/// <summary>
/// CommentarySystem 테스트용 스크립트
/// 키 입력으로 다양한 이벤트를 발생시켜 시스템을 테스트합니다.
/// </summary>
public class CommentarySystemTest : MonoBehaviour
{
    [SerializeField] private EventManager _eventManager;

    private void Update()
    {
        if (_eventManager == null) return;

        // 테스트 키 입력
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _eventManager.Publish(EventType.GameStart, "게임이 시작되었습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _eventManager.Publish(EventType.PatientCritical, "환자의 상태가 급격히 악화되고 있습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _eventManager.Publish(EventType.AssistDeliverItem, "어시스트가 제세동기를 전달했습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            _eventManager.Publish(EventType.MachineBroken, "장비가 고장났습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            _eventManager.Publish(EventType.SurgerySuccess, "수술이 성공했습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _eventManager.Publish(EventType.SurgeryFail, "수술이 실패했습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            _eventManager.Publish(EventType.TeamCooperation, "수술팀이 완벽하게 협동하고 있습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            _eventManager.Publish(EventType.PlayerMistake, "플레이어가 실수로 장비를 떨어뜨렸습니다.");
        }
    }
}
