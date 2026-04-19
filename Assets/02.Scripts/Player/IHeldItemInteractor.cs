using System;
using DontDillyDally.Data;
using Photon.Pun;
using UnityEngine;

public interface IHeldItemInteractor
{
    ItemObject CurrentHeldItem { get; }
    bool TryReleaseHeldItem(ItemObject expectedItem = null);
    bool TryConsumeHeldItem(ItemObject expectedItem = null);
    bool TryPickupInteractable(IInteractable interactable, Action onFailed = null, Func<bool> onBeforeHold = null);
    bool TryBeginHeldItemInteractionLock(ItemObject expectedHeldItem);
    void EndHeldItemInteractionLock();
    PhotonView GetInteractorPhotonView();
    Transform GetHandAttachPoint();
}
