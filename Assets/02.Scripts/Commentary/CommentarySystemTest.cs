using UnityEngine;

public class CommentarySystemTest : MonoBehaviour
{
    private void Update()
    {
        if (EventManager.Instance == null) return;

        // 완전 고정형
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EventManager.Instance.OnGameStart();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            EventManager.Instance.OnSurgerySuccess();

        if (Input.GetKeyDown(KeyCode.Alpha3))
            EventManager.Instance.OnSurgeryFail();

        if (Input.GetKeyDown(KeyCode.Alpha4))
            EventManager.Instance.OnPatientDeath();

        // 템플릿형
        if (Input.GetKeyDown(KeyCode.Alpha5))
            EventManager.Instance.OnPatientCritical("심박수가 급격히 떨어지고 있습니다.");

        if (Input.GetKeyDown(KeyCode.Alpha6))
            EventManager.Instance.OnMachineBroken("산소포화도 측정기");

        if (Input.GetKeyDown(KeyCode.Alpha7))
            EventManager.Instance.OnEmergencyEvent("수술실에 정전이 발생했습니다.");

        // 완전 동적형
        if (Input.GetKeyDown(KeyCode.Alpha8))
            EventManager.Instance.OnNewPatientAppeared("중증 외상 환자입니다.");

        if (Input.GetKeyDown(KeyCode.Alpha9))
            EventManager.Instance.OnChainAccident("연속으로 장비 고장과 재료 지연이 발생했습니다.");

        if (Input.GetKeyDown(KeyCode.Alpha0))
            EventManager.Instance.OnChainCooperation("팀원들이 빠르게 장비를 수리하고 환자 상태를 안정시켰습니다.");
    }
}
