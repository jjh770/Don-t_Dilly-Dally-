using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider))]
    public class TrashBoxInteractable : MonoBehaviourPun, IInteractable, IItemAcceptor
    {
        private readonly HashSet<int> _processingItemIds = new HashSet<int>();

        public bool IsInteracting => false;
        public Transform Transform => transform;

        /// <summary>
        /// 아이템이 쓰레기통에서 처리된 직후(사운드 재생과 동일 시점) 발행된다.
        /// VFX 등 부가 피드백 컴포넌트가 구독하여 사용한다.
        /// </summary>
        public event Action ItemTrashed;

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

            if (!ItemRecycleUtility.TryRecycle(heldItem))
            {
                return;
            }

            PlayTrashFeedback();
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
            PlayTrashFeedback();
        }

        private void PlayTrashFeedback()
        {
            SoundManager.Instance.Play(SFXKey.RecycleBin, SoundType.Local);
            ItemTrashed?.Invoke();
        }

        private IEnumerator ReleaseProcessingLockNextFrame(int itemInstanceId)
        {
            yield return null;
            _processingItemIds.Remove(itemInstanceId);
        }
    }
}
