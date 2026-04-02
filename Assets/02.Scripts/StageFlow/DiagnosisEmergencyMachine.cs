using DontDillyDally.Data;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public class DiagnosisEmergencyMachine : MonoBehaviour
    {
        [Header("Diagnosis")]
        [SerializeField] private DiagnosisScanType _machineType = DiagnosisScanType.None;
        [SerializeField] private float _patientDetectionRadius = 2.5f;

        public DiagnosisScanType MachineType => _machineType;

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

        private bool IsNearCurrentPatient(StageFlowManager stageFlowManager)
        {
            if (stageFlowManager == null)
            {
                return false;
            }

            PatientInteractable[] patients = FindObjectsOfType<PatientInteractable>(true);
            int currentPatientIndex = stageFlowManager.CurrentPatientIndex.Value;
            for (int i = 0; i < patients.Length; i++)
            {
                PatientInteractable patient = patients[i];
                if (patient == null || patient.PatientIndex != currentPatientIndex)
                {
                    continue;
                }

                float sqrDistance = (patient.transform.position - transform.position).sqrMagnitude;
                return sqrDistance <= _patientDetectionRadius * _patientDetectionRadius;
            }

            return false;
        }
    }
}
