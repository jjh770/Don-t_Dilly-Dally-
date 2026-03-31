using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class TrashBoxInteractable : MonoBehaviourPun, IInteractable, IItemAcceptor
    {
        private readonly HashSet<int> _processingItemIds = new HashSet<int>();

        public bool IsInteracting => false;
        public Transform Transform => transform;

        public void Interact(Transform interactor)
        {
            if (interactor == null)
            {
                return;
            }

            IHeldItemInteractor heldItemInteractor = interactor.GetComponent<IHeldItemInteractor>();
            if (heldItemInteractor == null)
            {
                return;
            }

            ItemObject heldItem = heldItemInteractor.CurrentHeldItem;
            if (!CanAcceptItem(heldItem))
            {
                return;
            }

            if (!heldItemInteractor.TryReleaseHeldItem(heldItem))
            {
                return;
            }

            if (ItemRecycleUtility.TryRecycle(heldItem))
            {
                return;
            }
        }

        public void StopInteract()
        {
        }

        public bool CanAcceptItem(ItemObject item)
        {
            return item != null;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
            {
                return;
            }

            TryTrashFromCollider(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null)
            {
                return;
            }

            TryTrashFromCollider(other);
        }

        private void TryTrashFromCollider(Collider other)
        {
            if (other == null)
            {
                return;
            }

            ItemObject itemObject = other.GetComponentInParent<ItemObject>();
            if (itemObject == null || !CanAcceptItem(itemObject))
            {
                return;
            }

            HoldableItem holdableItem = itemObject.GetComponent<HoldableItem>();
            if (holdableItem != null && (holdableItem.IsInteracting || holdableItem.IsStoredInContainer))
            {
                return;
            }

            int itemInstanceId = itemObject.GetInstanceID();
            if (!_processingItemIds.Add(itemInstanceId))
            {
                return;
            }

            StartCoroutine(ReleaseProcessingLockNextFrame(itemInstanceId));
            ItemRecycleUtility.TryRecycle(itemObject);
        }

        private IEnumerator ReleaseProcessingLockNextFrame(int itemInstanceId)
        {
            yield return null;
            _processingItemIds.Remove(itemInstanceId);
        }
    }
}
