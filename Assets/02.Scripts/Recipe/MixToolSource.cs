using UnityEngine;

namespace DontDillyDally.Data
{
    // 조합 도구를 무한히 꺼내주는 공급원 오브젝트입니다.
    // 공급원은 타입과 생성 설정만 들고 있고, 실제 아이템 외형은 생성된 아이템이 관리합니다.
    public class MixToolSource : MonoBehaviour
    {
        [Header("공급 도구 정보")]
        [Tooltip("이 공급원이 꺼내주는 ToolType")]
        public ToolType ToolType = ToolType.None;

        [Tooltip("실제로 생성할 홀더블 조합 도구 프리팹")]
        public MixToolItem SpawnedItemPrefab;

        [Tooltip("새 아이템을 배치할 위치")]
        public Transform SpawnPoint;

        [Tooltip("아이템을 집어가면 자동으로 다시 채울지 여부")]
        public bool AutoRespawn = true;

        private MixToolItem currentSpawnedItem;

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

        public void Initialize(ToolType toolType)
        {
            ToolType = toolType;
            EnsureSpawnedItem(forceRespawn: true);
        }

        public void SetToolType(ToolType toolType)
        {
            ToolType = toolType;
            EnsureSpawnedItem(forceRespawn: true);
        }

        private void EnsureSpawnedItem(bool forceRespawn = false)
        {
            if (SpawnedItemPrefab == null || ToolType == ToolType.None)
                return;

            if (forceRespawn && currentSpawnedItem != null && currentSpawnedItem.IsStillAt(GetSpawnParent()))
            {
                Destroy(currentSpawnedItem.gameObject);
                currentSpawnedItem = null;
            }

            if (currentSpawnedItem != null)
                return;

            Transform parent = GetSpawnParent();
            MixToolItem spawnedItem = Instantiate(
                SpawnedItemPrefab,
                parent.position,
                parent.rotation,
                parent);

            spawnedItem.Initialize(ToolType);
            spawnedItem.name = string.IsNullOrWhiteSpace(spawnedItem.DisplayName)
                ? ToolType.ToString()
                : spawnedItem.DisplayName;

            currentSpawnedItem = spawnedItem;
        }

        private Transform GetSpawnParent()
        {
            return SpawnPoint != null ? SpawnPoint : transform;
        }
    }
}
