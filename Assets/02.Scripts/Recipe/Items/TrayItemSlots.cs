using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 트레이 위의 월드 아이템 배치와 슬롯 시각 표현만 담당합니다.
    public class TrayItemSlots : MonoBehaviour
    {
        private const int MaxItemSlots = 4;

        [SerializeField] private Transform[] _itemSlotPoints = new Transform[MaxItemSlots];

        private ItemObject[] _storedSlotItems;
        private Vector3[] _defaultSlotLocalPositions;

        private void Awake()
        {
            EnsureStoredSlotItems();
            CacheDefaultSlotLocalPositions();
        }

        public int GetFirstAvailableSlotIndex()
        {
            EnsureStoredSlotItems();

            for (int i = 0; i < _storedSlotItems.Length; i++)
            {
                if (_storedSlotItems[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public Transform GetSlotTransform(int slotIndex)
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

        public bool TryStoreItem(ItemObject itemObject, int slotIndex)
        {
            EnsureStoredSlotItems();
            if (itemObject == null)
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
            ResetSlotPosition(slotIndex);
            PlaceStoredItem(itemObject, slotTransform);
            SetStoredItemInteractionEnabled(itemObject, false);
            _storedSlotItems[slotIndex] = itemObject;
            return true;
        }

        public void ClearStoredItems()
        {
            EnsureStoredSlotItems();

            for (int i = 0; i < _storedSlotItems.Length; i++)
            {
                ItemObject storedItem = _storedSlotItems[i];
                if (storedItem == null)
                {
                    continue;
                }

                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.Destroy(storedItem.gameObject);
                }
                else
                {
                    Destroy(storedItem.gameObject);
                }

                _storedSlotItems[i] = null;
                ResetSlotPosition(i);
            }
        }

        private void EnsureStoredSlotItems()
        {
            if (_storedSlotItems == null || _storedSlotItems.Length != MaxItemSlots)
            {
                _storedSlotItems = new ItemObject[MaxItemSlots];
            }
        }

        private void CacheDefaultSlotLocalPositions()
        {
            _defaultSlotLocalPositions = new Vector3[MaxItemSlots];

            for (int i = 0; i < MaxItemSlots; i++)
            {
                Transform slotTransform = GetSlotTransform(i);
                _defaultSlotLocalPositions[i] = slotTransform.localPosition;
            }
        }

        private void ResetSlotPosition(int slotIndex)
        {
            if (_defaultSlotLocalPositions == null ||
                slotIndex < 0 ||
                slotIndex >= _defaultSlotLocalPositions.Length)
            {
                return;
            }

            Transform slotTransform = GetSlotTransform(slotIndex);
            slotTransform.localPosition = _defaultSlotLocalPositions[slotIndex];
        }

        private static void SetStoredItemInteractionEnabled(ItemObject itemObject, bool isEnabled)
        {
            if (itemObject == null)
            {
                return;
            }

            Collider[] colliders = itemObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                col.enabled = isEnabled;
            }
        }

        private static void PlaceStoredItem(ItemObject itemObject, Transform slotTransform)
        {
            if (itemObject == null || slotTransform == null)
            {
                return;
            }

            if (itemObject.TryGetComponent(out HoldableItem holdableItem))
            {
                holdableItem.Place(slotTransform);
            }
            else
            {
                itemObject.transform.SetPositionAndRotation(slotTransform.position, slotTransform.rotation);
            }

            itemObject.transform.SetParent(slotTransform, false);
            itemObject.transform.localPosition = Vector3.zero;
            itemObject.transform.localRotation = Quaternion.identity;

            BoxCollider placementCollider = itemObject.TargetBoxCollider != null ? itemObject.TargetBoxCollider : itemObject.GetComponent<BoxCollider>();

            if (placementCollider != null)
            {
                float bottomOffset = placementCollider.center.y - placementCollider.size.y * 0.5f;
                slotTransform.position += Vector3.up * -bottomOffset;
            }
        }
    }
}
