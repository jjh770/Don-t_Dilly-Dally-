using UnityEngine;

namespace DontDillyDally.Data
{
    internal static class MachineStoredItemUtility
    {
        public static void StoreInMachine(ItemObject itemObject, Transform slotTransform)
        {
            if (itemObject == null || slotTransform == null)
            {
                return;
            }

            if (itemObject.TryGetComponent(out HoldableItem holdableItem))
            {
                holdableItem.Place(slotTransform);
                holdableItem.SetStoredInContainer(true);
                holdableItem.SetAllCollidersEnabled(false);
            }

            itemObject.transform.SetParent(slotTransform, true);
            NetworkItemOwnership.ReturnOwnershipToMaster(itemObject.PhotonView);
        }

        public static void PrepareForPickup(ItemObject itemObject)
        {
            if (itemObject == null)
            {
                return;
            }

            if (itemObject.TryGetComponent(out HoldableItem holdableItem))
            {
                holdableItem.SetStoredInContainer(false);
            }

            itemObject.transform.SetParent(null, true);
        }
    }
}
