using DontDillyDally.Data;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [RequireComponent(typeof(Collider))]
    public class PatientInteractable : MonoBehaviour, IInteractable
    {
        [Header("환자 설정")]
        [SerializeField] private int _patientIndex;
        [SerializeField] private bool _allowOnlyCurrentPatient = true;
        [SerializeField] private bool _surgeonOnly = true;

        public bool IsInteracting => false;
        public Transform Transform => transform;

        public void Interact(Transform interactor)
        {
            if (interactor == null)
            {
                return;
            }

            StageFlowManager stageFlowManager = StageFlowManager.Instance;
            if (stageFlowManager == null || !stageFlowManager.CanLocalInteractWithPatient)
            {
                return;
            }

            if (_surgeonOnly && !stageFlowManager.IsLocalSurgeon)
            {
                return;
            }

            if (_allowOnlyCurrentPatient &&
                stageFlowManager.CurrentPatientIndex.Value != _patientIndex)
            {
                return;
            }

            PlayerInteractionAbility interactionAbility = interactor.GetComponent<PlayerInteractionAbility>();
            if (interactionAbility == null)
            {
                return;
            }

            TrayItem trayItem = interactionAbility.CurrentHeldItem as TrayItem;
            if (trayItem == null)
            {
                return;
            }

            SubmittedTray traySnapshot = trayItem.GetTraySnapshot();
            if (traySnapshot == null)
            {
                return;
            }

            int trayViewId = -1;
            if (trayItem.TryGetComponent(out Photon.Pun.PhotonView trayView))
            {
                trayViewId = trayView.ViewID;
            }

            if (!stageFlowManager.RequestTraySubmission(traySnapshot, trayViewId))
            {
                return;
            }

            trayItem.ClearContentsAndSync();
        }

        public void StopInteract()
        {
        }
    }
}
