using System;
using DontDillyDally.Data;

public interface IHeldItemInteractor
{
    ItemObject CurrentHeldItem { get; }
    bool TryReleaseHeldItem(ItemObject expectedItem = null);
    bool TryConsumeHeldItem(ItemObject expectedItem = null);
    bool TryPickupInteractable(IInteractable interactable, Action onFailed = null);
    bool TryBeginHeldItemInteractionLock(ItemObject expectedHeldItem);
    void EndHeldItemInteractionLock();
}
