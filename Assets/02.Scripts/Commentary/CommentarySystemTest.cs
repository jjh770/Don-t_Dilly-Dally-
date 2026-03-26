using UnityEngine;

public class CommentarySystemTest : MonoBehaviour
{
    [SerializeField] private CommentaryEventPublisher _publisher;

    private void Update()
    {
        if (_publisher == null) return;

        // 완전 고정형
        if (Input.GetKeyDown(KeyCode.Alpha1))
            _publisher.OnGameStart();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            _publisher.OnSurgerySuccess();

        if (Input.GetKeyDown(KeyCode.Alpha3))
            _publisher.OnSurgeryFail();

        if (Input.GetKeyDown(KeyCode.Alpha4))
            _publisher.OnPatientDeath();

        // 템플릿형
        if (Input.GetKeyDown(KeyCode.Alpha5))
            _publisher.OnPatientCritical("심박수가 급격히 떨어지고 있습니다.");

        if (Input.GetKeyDown(KeyCode.Alpha6))
            _publisher.OnMachineBroken("산소포화도 측정기");

        if (Input.GetKeyDown(KeyCode.Alpha7))
            _publisher.OnEmergencyEvent("수술실에 정전이 발생했습니다.");

        // 완전 동적형
        if (Input.GetKeyDown(KeyCode.Alpha8))
            _publisher.OnNewPatientAppeared("중증 외상 환자입니다.");

        if (Input.GetKeyDown(KeyCode.Alpha9))
            _publisher.OnChainAccident("연속으로 장비 고장과 재료 지연이 발생했습니다.");

        if (Input.GetKeyDown(KeyCode.Alpha0))
            _publisher.OnChainCooperation("팀원들이 빠르게 장비를 수리하고 환자 상태를 안정시켰습니다.");
    }
}
