using UnityEngine;

// 해설 이벤트 발행을 위한 래퍼 클래스
// 다른 스크립트에서는 이 클래스의 메서드를 호출하여 이벤트 발행
public class CommentaryEventPublisher : MonoBehaviour
{
    public static CommentaryEventPublisher Instance { get; private set; }

    [SerializeField] private EventManager _eventManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void OnGameStart()
    {
        Publish(EventType.GameStart, "게임이 시작되었습니다.");
    }

    public void OnGameOver()
    {
        Publish(EventType.GameOver, "게임이 종료되었습니다.");
    }

    public void OnSurgerySuccess()
    {
        Publish(EventType.SurgerySuccess, "수술이 성공적으로 완료되었습니다.");
    }

    public void OnSurgeryFail()
    {
        Publish(EventType.SurgeryFail, "수술에 실패했습니다.");
    }

    public void OnPatientDeath()
    {
        Publish(EventType.PatientDeath, "환자가 사망했습니다.");
    }

    public void OnEquipmentAccident(string equipmentName)
    {
        Publish(EventType.EquipmentAccident, $"{equipmentName} 장비에 사고가 발생했습니다.");
    }

    public void OnPatientCritical(string detail = null)
    {
        string description = string.IsNullOrEmpty(detail)
            ? "환자의 상태가 위험합니다."
            : $"환자의 상태가 위험합니다. {detail}";
        Publish(EventType.PatientCritical, description);
    }

    public void OnEmergencyEvent()
    {
        Publish(EventType.EmergencyEvent, $"긴급 상황 발생");
    }

    public void OnEmergencyEvent(string emergencyDetail)
    {
        Publish(EventType.EmergencyEvent, $"긴급 상황 발생: {emergencyDetail}");
    }

    public void OnMachineBroken(string machineName)
    {
        Publish(EventType.MachineBroken, $"{machineName} 기계가 고장났습니다.");
    }

    public void OnNewPatientAppeared(string patientInfo)
    {
        Publish(EventType.NewPatientAppeared, $"새로운 환자가 등장했습니다. {patientInfo}");
    }

    public void OnMaterialDeliveredLate(string materialName)
    {
        Publish(EventType.MaterialDeliveredLate, $"{materialName} 재료가 늦게 전달되었습니다.");
    }

    public void OnEmergencyPrevented(string emergencyDetail)
    {
        Publish(EventType.EmergencyPrevented, $"긴급 이벤트를 막아냈습니다. {emergencyDetail}");
    }

    public void OnWrongMaterialUsed(string materialName)
    {
        Publish(EventType.WrongMaterialUsed, $"잘못된 재료를 사용했습니다: {materialName}");
    }

    public void OnRepairTimeout(string machineName)
    {
        Publish(EventType.RepairTimeout, $"제한 시간 내에 {machineName} 장비를 고치지 못했습니다.");
    }

    public void OnRepairCompletedFast(string machineName)
    {
        Publish(EventType.RepairCompletedFast, $"{machineName} 장비 수리를 빠르게 완료했습니다.");
    }

    public void OnRepairCompletedLate(string machineName)
    {
        Publish(EventType.RepairCompletedLate, $"{machineName} 장비 수리가 늦어졌습니다.");
    }

    public void OnChainAccident(string accidentContext)
    {
        Publish(EventType.ChainAccident, accidentContext);
    }

    public void OnChainCooperation(string cooperationContext)
    {
        Publish(EventType.ChainCooperation, cooperationContext);
    }

    private void Publish(EventType type, string description)
    {
        if (_eventManager == null)
        {
            Debug.LogWarning("[CommentaryEventPublisher] EventManager가 설정되지 않았습니다.");
            return;
        }
        _eventManager.Publish(type, description);
    }
}
