using UnityEngine;

namespace DontDillyDally.Data
{
    // 기본 재료를 무한히 꺼내주는 공급원 오브젝트입니다.
    // 공급원은 타입과 생성 설정만 들고 있고, 실제 아이템 외형은 생성된 아이템이 관리합니다.
    public class BasicMaterialSource : MonoBehaviour
    {
        [Header("공급 재료 정보")]
        [Tooltip("이 공급원이 꺼내주는 기본 재료 타입")]
        public CraftedMaterialType MaterialType = CraftedMaterialType.None;

        [Tooltip("실제로 생성할 홀더블 기본 재료 프리팹")]
        public BasicMaterialItem SpawnedItemPrefab;

        [Tooltip("새 아이템을 배치할 위치")]
        public Transform SpawnPoint;

        [Tooltip("아이템을 집어가면 자동으로 다시 채울지 여부")]
        public bool AutoRespawn = true;

        private BasicMaterialItem currentSpawnedItem;

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

            if (!currentSpawnedItem.IsStillAt(GetSpawnParent()))
            {
                currentSpawnedItem = null;
                EnsureSpawnedItem();
            }
        }

        public void Initialize(CraftedMaterialType materialType)
        {
            MaterialType = materialType;
            EnsureSpawnedItem(forceRespawn: true);
        }

        public void SetMaterialType(CraftedMaterialType materialType)
        {
            MaterialType = materialType;
            EnsureSpawnedItem(forceRespawn: true);
        }

        private void EnsureSpawnedItem(bool forceRespawn = false)
        {
            if (SpawnedItemPrefab == null || MaterialType == CraftedMaterialType.None)
                return;

            if (forceRespawn && currentSpawnedItem != null && currentSpawnedItem.IsStillAt(GetSpawnParent()))
            {
                Destroy(currentSpawnedItem.gameObject);
                currentSpawnedItem = null;
            }

            if (currentSpawnedItem != null)
                return;

            Transform parent = GetSpawnParent();
            BasicMaterialItem spawnedItem = Instantiate(
                SpawnedItemPrefab,
                parent.position,
                parent.rotation,
                parent);

            spawnedItem.Initialize(MaterialType);
            spawnedItem.name = string.IsNullOrWhiteSpace(spawnedItem.DisplayName)
                ? MaterialType.ToString()
                : spawnedItem.DisplayName;

            currentSpawnedItem = spawnedItem;
        }

        private Transform GetSpawnParent()
        {
            return SpawnPoint != null ? SpawnPoint : transform;
        }
    }
}
