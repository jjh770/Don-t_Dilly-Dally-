using UnityEngine;

namespace DontDillyDally.Data
{
    // 트레이를 무한 공급하는 공급원 오브젝트입니다.
    // 생성 설정만 들고 있고, 실제 트레이 아이템 생성과 재생성만 담당합니다.
    public class TraySource : MonoBehaviour
    {
        [Header("공급 설정")]
        [Tooltip("실제로 생성할 트레이 아이템 프리팹")]
        public TrayItem SpawnedItemPrefab;

        [Tooltip("트레이 아이템을 배치할 위치")]
        public Transform SpawnPoint;

        [Tooltip("트레이를 집어가면 자동으로 다시 채울지 여부")]
        public bool AutoRespawn = true;

        private TrayItem currentSpawnedItem;

        private void Start()
        {
            EnsureSpawnedItem();
        }

        private void Update()
        {
            if (!AutoRespawn)
                return;

            if (currentSpawnedItem == null)
            {
                EnsureSpawnedItem();
                return;
            }

            if (currentSpawnedItem.transform.parent != GetSpawnParent())
            {
                currentSpawnedItem = null;
                EnsureSpawnedItem();
            }
        }

        public void ForceRespawn()
        {
            EnsureSpawnedItem(forceRespawn: true);
        }

        private void EnsureSpawnedItem(bool forceRespawn = false)
        {
            if (SpawnedItemPrefab == null)
                return;

            if (forceRespawn && currentSpawnedItem != null && currentSpawnedItem.transform.parent == GetSpawnParent())
            {
                Destroy(currentSpawnedItem.gameObject);
                currentSpawnedItem = null;
            }

            if (currentSpawnedItem != null)
                return;

            Transform parent = GetSpawnParent();
            TrayItem spawnedItem = Instantiate(
                SpawnedItemPrefab,
                parent.position,
                parent.rotation,
                parent);

            spawnedItem.ResetTrayData(false);
            spawnedItem.name = string.IsNullOrWhiteSpace(spawnedItem.DisplayName)
                ? "Tray"
                : spawnedItem.DisplayName;

            currentSpawnedItem = spawnedItem;
        }

        private Transform GetSpawnParent()
        {
            return SpawnPoint != null ? SpawnPoint : transform;
        }
    }
}
