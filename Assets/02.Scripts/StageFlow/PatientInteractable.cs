using DontDillyDally.Data;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [RequireComponent(typeof(Collider))]
    public class PatientInteractable : MonoBehaviour, IInteractable, IItemAcceptor
    {
        [Header("환자 설정")]
        [SerializeField] private bool _surgeonOnly = true;

        public int PatientIndex =>
            StageFlowManager.Instance != null ? StageFlowManager.Instance.CurrentPatientIndex.Value : 0;
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

            PlayerInteractionAbility interactionAbility = interactor.GetComponent<PlayerInteractionAbility>();
            if (interactionAbility == null)
            {
                return;
            }

            IHeldItemInteractor heldItemInteractor = interactor.GetComponent<IHeldItemInteractor>();
            ItemObject heldItem = interactionAbility.CurrentHeldItem;

            if (stageFlowManager.IsEmergencyActive &&
                stageFlowManager.CurrentEmergencyKind == EmergencyEventKind.Tray)
            {
                BasicMaterialItem materialItem = heldItem as BasicMaterialItem;
                if (materialItem == null)
                {
                    return;
                }

                int itemViewId = materialItem.ViewId;

                if (!stageFlowManager.RequestEmergencyMaterialSubmission(materialItem.MaterialType, itemViewId))
                {
                    return;
                }

                heldItemInteractor?.TryConsumeHeldItem(materialItem);
                return;
            }

            TrayItem trayItem = heldItem as TrayItem;
            if (trayItem == null)
            {
                return;
            }

            SubmittedTray traySnapshot = trayItem.GetTraySnapshot();
            if (traySnapshot == null)
            {
                return;
            }

            int trayViewId = trayItem.ViewId;

            if (!stageFlowManager.RequestTraySubmission(traySnapshot, trayViewId))
            {
                return;
            }

            heldItemInteractor?.TryConsumeHeldItem(trayItem);
        }

        public void StopInteract()
        {
        }

        public bool CanAcceptItem(ItemObject item)
        {
            StageFlowManager stageFlowManager = StageFlowManager.Instance;
            if (stageFlowManager == null || !stageFlowManager.CanLocalInteractWithPatient)
                return false;

            if (_surgeonOnly && !stageFlowManager.IsLocalSurgeon)
                return false;

            if (stageFlowManager.IsEmergencyActive &&
                stageFlowManager.CurrentEmergencyKind == EmergencyEventKind.Tray)
            {
                return item is BasicMaterialItem;
            }

            return item is TrayItem;
        }
    }
}
