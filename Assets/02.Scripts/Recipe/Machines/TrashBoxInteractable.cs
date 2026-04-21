using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    [RequireComponent(typeof(Collider), typeof(PhotonView))]
    public class TrashBoxInteractable : MonoBehaviourPun, IInteractable, IItemAcceptor
    {
        private readonly HashSet<int> _processingItemIds = new HashSet<int>();

        public bool IsInteracting => false;
        public Transform Transform => transform;

        // 아이템이 쓰레기통에서 처리된 직후(사운드 재생과 동일 시점) 발행된다.
        // VFX 등 부가 피드백 컴포넌트가 구독하여 사용한다.
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

            // 멀티플레이어에서 리모트 클라이언트의 물리 lerp로 트리거가 중복 발생해도
            // 피드백이 겹치지 않도록, 아이템 소유 클라이언트에서만 처리한다.
            if (!CanInitiateTrashForItem(itemObject))
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

        private static bool CanInitiateTrashForItem(ItemObject itemObject)
        {
            // 오프라인(룸 미참여) 상태에서는 로컬 클라이언트가 단독으로 처리한다.
            if (!PhotonNetwork.InRoom)
            {
                return true;
            }

            PhotonView itemPhotonView = itemObject.PhotonView;
            if (itemPhotonView == null)
            {
                return false;
            }

            return itemPhotonView.IsMine;
        }

        private void PlayTrashFeedback()
        {
            // SoundType.Local은 본인 전용 피드백이므로 리모트 동기화 대상이 아니다.
            SoundManager.Instance.Play(SFXKey.RecycleBin, SoundType.Local);

            RaiseItemTrashed();
            BroadcastItemTrashedToRemotes();
        }

        private void RaiseItemTrashed()
        {
            ItemTrashed?.Invoke();
        }

        private void BroadcastItemTrashedToRemotes()
        {
            if (!PhotonNetwork.InRoom || photonView == null)
            {
                return;
            }

            photonView.RPC(nameof(RPC_OnItemTrashed), RpcTarget.Others);
        }

        [PunRPC]
        private void RPC_OnItemTrashed()
        {
            RaiseItemTrashed();
        }

        private IEnumerator ReleaseProcessingLockNextFrame(int itemInstanceId)
        {
            yield return null;
            _processingItemIds.Remove(itemInstanceId);
        }
    }
}
