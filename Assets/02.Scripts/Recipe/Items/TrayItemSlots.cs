using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 트레이 위 아이템 배치와 슬롯별 시각 정렬을 담당합니다.
    public class TrayItemSlots : MonoBehaviour
    {
        private const int MaxItemSlots = 4;
        [SerializeField] private Transform[] _itemSlotPoints = new Transform[MaxItemSlots];

        private ItemObject[] _storedSlotItems;
        private CraftedItem[] _storedSlotData;
        private Vector3[] _storedLocalPositions;
        private TrayItem _trayItem;

        private void Awake()
        {
            _trayItem = GetComponent<TrayItem>();
            _storedSlotItems = new ItemObject[MaxItemSlots];
            _storedSlotData = new CraftedItem[MaxItemSlots];
            _storedLocalPositions = new Vector3[MaxItemSlots];
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _storedSlotItems.Length; i++)
            {
                ItemObject item = _storedSlotItems[i];
                if (item == null)
                {
                    continue;
                }

                Transform expectedParent = GetSlotTransform(i);
                if (IsStaleStoredItem(item, expectedParent))
                {
                    LogStoredSlot("LateUpdate stale reference cleared", i, item, expectedParent);
                    ClearSlot(i, removeTrayData: true);
                    continue;
                }

                item.transform.localPosition = _storedLocalPositions[i];
                item.transform.localRotation = Quaternion.identity;
            }
        }

        public int GetFirstAvailableSlotIndex()
        {
            for (int i = 0; i < _storedSlotItems.Length; i++)
            {
                if (_storedSlotItems[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool HasStoredItems()
        {
            if (_storedSlotItems == null)
            {
                return false;
            }

            for (int i = 0; i < _storedSlotItems.Length; i++)
            {
                if (_storedSlotItems[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSlotAvailable(int slotIndex)
        {
            if (_storedSlotItems == null)
            {
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _storedSlotItems.Length)
            {
                return false;
            }

            return _storedSlotItems[slotIndex] == null;
        }

        public bool TryStoreItem(ItemObject itemObject, int slotIndex, CraftedItem itemData)
        {
            if (itemObject == null || itemData == null)
            {
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _storedSlotItems.Length)
            {
                return false;
            }

            if (_storedSlotItems[slotIndex] != null)
            {
                return false;
            }

            Transform slotTransform = GetSlotTransform(slotIndex);
            PlaceStoredItem(itemObject, slotTransform);
            DisableItemInteraction(itemObject);
            _storedSlotItems[slotIndex] = itemObject;
            _storedSlotData[slotIndex] = itemData;
            _storedLocalPositions[slotIndex] = itemObject.transform.localPosition;
            itemObject.Recycled += OnStoredItemRecycled;
            return true;
        }

        // 풀 반납된 아이템이 다른 역할로 재사용될 때 stale 참조가 LateUpdate에서
        // localPosition을 덮어써 (0, 0.2, 0)에 박히는 버그 방지.
        private void OnStoredItemRecycled(ItemObject item)
        {
            if (item == null || _storedSlotItems == null)
            {
                return;
            }

            for (int i = 0; i < _storedSlotItems.Length; i++)
            {
                if (_storedSlotItems[i] == item)
                {
                    ClearSlot(i, removeTrayData: true);
                }
            }
        }

        private void ClearSlot(int slotIndex, bool removeTrayData = false)
        {
            if (_storedSlotItems == null || slotIndex < 0 || slotIndex >= _storedSlotItems.Length)
            {
                return;
            }

            ItemObject item = _storedSlotItems[slotIndex];
            if (item != null)
            {
                item.Recycled -= OnStoredItemRecycled;
            }

            _storedSlotItems[slotIndex] = null;
            if (_storedSlotData != null)
            {
                _storedSlotData[slotIndex] = null;
            }
            _storedLocalPositions[slotIndex] = Vector3.zero;

            if (removeTrayData)
            {
                _trayItem?.RebuildStoredItemData(_storedSlotData);
            }
        }

        /// <summary>
        /// 현재 보관 중인 아이템들의 PhotonView ID 목록을 반환합니다.
        /// 아이템이 없으면 null을 반환합니다.
        /// </summary>
        public int[] GetStoredItemViewIds()
        {
            if (_storedSlotItems == null)
            {
                return null;
            }

            int count = 0;
            int[] viewIds = new int[MaxItemSlots];

            for (int i = 0; i < _storedSlotItems.Length; i++)
            {
                if (_storedSlotItems[i] == null)
                {
                    continue;
                }

                PhotonView pv = _storedSlotItems[i].PhotonView;
                if (pv != null)
                {
                    viewIds[count++] = pv.ViewID;
                }
            }

            if (count == 0)
            {
                return System.Array.Empty<int>();
            }

            return viewIds[..count];
        }

        /// <summary>
        /// 보관 중인 아이템을 모두 정리합니다.
        /// 로컬에서 직접 회수하지 못한 아이템은 ViewID 목록으로 반환합니다.
        /// </summary>
        public int[] ClearStoredItems()
        {
            if (_storedSlotItems == null)
            {
                return null;
            }

            int undestroyedCount = 0;
            int[] undestroyedViewIds = new int[MaxItemSlots];

            for (int i = 0; i < _storedSlotItems.Length; i++)
            {
                ItemObject storedItem = _storedSlotItems[i];
                _storedSlotItems[i] = null;
                if (_storedSlotData != null)
                {
                    _storedSlotData[i] = null;
                }
                _storedLocalPositions[i] = Vector3.zero;

                if (storedItem == null)
                {
                    continue;
                }

                storedItem.Recycled -= OnStoredItemRecycled;

                // 트레이가 먼저 파괴되더라도 자식 PhotonView가 로컬에서 함께 지워지지 않도록
                // 보관 중이던 아이템을 먼저 트레이 계층에서 분리합니다.
                storedItem.transform.SetParent(null, true);

                PhotonView photonView = storedItem.PhotonView;
                if (PhotonNetwork.InRoom && photonView != null)
                {
                    if (ItemRecycleUtility.TryRecycle(storedItem))
                    {
                        continue;
                    }

                    // 소유권이 없어 직접 회수하지 못한 아이템만 따로 수집합니다.
                    undestroyedViewIds[undestroyedCount++] = photonView.ViewID;
                    continue;
                }

                ItemRecycleUtility.TryRecycle(storedItem);
            }

            if (undestroyedCount == 0)
            {
                return System.Array.Empty<int>();
            }

            return undestroyedViewIds[..undestroyedCount];
        }

        private Transform GetSlotTransform(int slotIndex)
        {
            if (_itemSlotPoints != null &&
                slotIndex >= 0 &&
                slotIndex < _itemSlotPoints.Length &&
                _itemSlotPoints[slotIndex] != null)
            {
                return _itemSlotPoints[slotIndex];
            }

            return transform;
        }

        private bool IsStaleStoredItem(ItemObject item, Transform expectedParent)
        {
            if (item == null)
            {
                return false;
            }

            bool isInactive = !item.gameObject.activeInHierarchy;
            bool parentMismatch = item.transform.parent != expectedParent;
            bool isNoLongerStored = item.TryGetComponent(out HoldableItem holdable) && !holdable.IsStoredInContainer;

            return isInactive || item.IsPendingRecycle || parentMismatch || isNoLongerStored;
        }

        private void LogStoredSlot(string context, int slotIndex, ItemObject item, Transform expectedParent)
        {
            if (item == null)
            {
                return;
            }

            bool hasHoldable = item.TryGetComponent(out HoldableItem holdable);
            string expectedParentName = expectedParent != null ? expectedParent.name : "null";
            Transform actualParent = item.transform.parent;
            string actualParentName = actualParent != null ? actualParent.name : "null";

            Debug.LogWarning(
                $"[TrayItemSlots] {context} | tray={name} slot={slotIndex} " +
                $"item={item.name} viewId={item.ViewId} active={item.gameObject.activeInHierarchy} " +
                $"pendingRecycle={item.IsPendingRecycle} stored={(hasHoldable && holdable.IsStoredInContainer)} " +
                $"expectedParent={expectedParentName} actualParent={actualParentName} " +
                $"local={item.transform.localPosition} world={item.transform.position}");
        }

        private static void DisableItemInteraction(ItemObject itemObject)
        {
            if (itemObject == null)
            {
                return;
            }

            if (itemObject.TryGetComponent(out HoldableItem holdable))
            {
                holdable.SetAllCollidersEnabled(false);
                holdable.SetStoredInContainer(true);
            }
        }

        private static void PlaceStoredItem(ItemObject itemObject, Transform slotTransform)
        {
            if (itemObject == null || slotTransform == null)
            {
                return;
            }

            bool isLocalOwner = itemObject.PhotonView != null && itemObject.PhotonView.IsMine;

            if (itemObject.TryGetComponent(out HoldableItem holdableItem))
            {
                if (isLocalOwner)
                {
                    holdableItem.Place(slotTransform);
                }
                else
                {
                    // 비소유자 클라이언트에서도 홀드 상태를 해제하여
                    // UpdateHeldTransform이 위치를 손으로 덮어쓰지 않도록 합니다.
                    holdableItem.ApplyNetworkHoldState(false, -1);
                }

                holdableItem.SetStoredInContainer(true);
            }

            itemObject.transform.SetParent(slotTransform, false);
            itemObject.transform.localRotation = Quaternion.identity;

            BoxCollider placementCollider = itemObject.TargetBoxCollider != null ? itemObject.TargetBoxCollider : itemObject.GetComponent<BoxCollider>();

            Vector3 localPosition = Vector3.zero;
            if (placementCollider != null)
            {
                float bottomOffset = placementCollider.center.y - placementCollider.size.y * 0.5f;
                localPosition.y = -bottomOffset;
            }
            itemObject.transform.localPosition = localPosition;

            NetworkItemOwnership.ReturnOwnershipToMaster(itemObject.PhotonView);
        }
    }
}
