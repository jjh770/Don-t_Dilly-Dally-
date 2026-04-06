using System.Collections.Generic;
using DontDillyDally.Data;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public class DiagnosisEmergencyMachine : MonoBehaviour
    {
        [Header("Diagnosis")]
        [SerializeField] private DiagnosisScanType _machineType = DiagnosisScanType.None;

        public DiagnosisScanType MachineType => _machineType;

        private readonly Dictionary<int, int> _patientZoneCounts = new Dictionary<int, int>();

        public bool TryHandleEmergencyInteract(Transform interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            StageFlowManager stageFlowManager = StageFlowManager.Instance;
            if (stageFlowManager == null ||
                !stageFlowManager.IsLocalSurgeon ||
                !stageFlowManager.IsEmergencyActive ||
                stageFlowManager.CurrentEmergencyKind != EmergencyEventKind.Diagnosis)
            {
                return false;
            }

            if (!IsNearCurrentPatient(stageFlowManager))
            {
                return false;
            }

            return stageFlowManager.RequestDiagnosisOperation(_machineType);
        }

        public bool ShouldShowOperationTimerUi(StageFlowManager stageFlowManager)
        {
            if (stageFlowManager == null ||
                !stageFlowManager.IsLocalSurgeon ||
                !stageFlowManager.IsEmergencyActive ||
                stageFlowManager.CurrentEmergencyKind != EmergencyEventKind.Diagnosis ||
                !stageFlowManager.IsEmergencyDiagnosisOperating)
            {
                return false;
            }

            return stageFlowManager.CurrentEmergencyDiagnosisTarget == _machineType;
        }

        private bool IsNearCurrentPatient(StageFlowManager stageFlowManager)
        {
            if (stageFlowManager == null)
            {
                return false;
            }

            int currentPatientIndex = stageFlowManager.CurrentPatientIndex.Value;
            return _patientZoneCounts.TryGetValue(currentPatientIndex, out int count) && count > 0;
        }

        public void NotifyPatientZoneEntered(int patientIndex)
        {
            if (patientIndex < 0)
            {
                return;
            }

            if (_patientZoneCounts.TryGetValue(patientIndex, out int count))
            {
                _patientZoneCounts[patientIndex] = count + 1;
            }
            else
            {
                _patientZoneCounts[patientIndex] = 1;
            }
        }

        public void NotifyPatientZoneExited(int patientIndex)
        {
            if (!_patientZoneCounts.TryGetValue(patientIndex, out int count))
            {
                return;
            }

            if (count <= 1)
            {
                _patientZoneCounts.Remove(patientIndex);
                return;
            }

            _patientZoneCounts[patientIndex] = count - 1;
        }
    }
}
