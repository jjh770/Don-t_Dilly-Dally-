using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [RequireComponent(typeof(Collider))]
    public class DiagnosisEmergencyPatientZone : MonoBehaviour
    {
        [SerializeField] private PatientInteractable _patientInteractable;

        private Collider _triggerCollider;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider>();
            _triggerCollider.isTrigger = true;

            if (_patientInteractable == null)
            {
                _patientInteractable = GetComponentInParent<PatientInteractable>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetMachine(other, out DiagnosisEmergencyMachine machine))
            {
                return;
            }

            machine.NotifyPatientZoneEntered(GetPatientIndex());
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetMachine(other, out DiagnosisEmergencyMachine machine))
            {
                return;
            }

            machine.NotifyPatientZoneExited(GetPatientIndex());
        }

        private bool TryGetMachine(Collider other, out DiagnosisEmergencyMachine machine)
        {
            machine = null;
            if (other == null)
            {
                return false;
            }

            machine = other.GetComponentInParent<DiagnosisEmergencyMachine>();
            return machine != null;
        }

        private int GetPatientIndex()
        {
            return _patientInteractable != null ? _patientInteractable.PatientIndex : -1;
        }
    }
}
