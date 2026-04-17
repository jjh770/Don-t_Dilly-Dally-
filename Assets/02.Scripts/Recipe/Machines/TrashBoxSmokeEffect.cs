using UnityEngine;

namespace DontDillyDally.Data
{
    /// <summary>
    /// 쓰레기통 피드백용 연기 VFX 재생 컴포넌트.
    /// TrashBoxInteractable.ItemTrashed 이벤트를 구독하여 아이템이
    /// 버려질 때마다 연기 파티클을 스폰한다. (E키 / 던지기 공통)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrashBoxSmokeEffect : MonoBehaviour
    {
        [Header("VFX")]
        [Tooltip("아이템이 버려질 때 재생할 연기 파티클 프리팹")]
        [SerializeField] private ParticleSystem _smokePrefab;

        [Tooltip("연기 스폰 위치. 비워두면 이 오브젝트의 Transform 사용")]
        [SerializeField] private Transform _spawnPoint;

        [Tooltip("스폰된 파티클 인스턴스를 파괴하기까지 대기 시간 (초)")]
        [SerializeField, Min(0.1f)] private float _destroyDelay = 3f;

        [Header("참조")]
        [Tooltip("구독 대상 쓰레기통. 같은 오브젝트의 컴포넌트가 자동 할당됨")]
        [SerializeField] private TrashBoxInteractable _trashBox;

        private void Reset()
        {
            _trashBox = GetComponent<TrashBoxInteractable>();
        }

        private void Awake()
        {
            if (_trashBox == null)
            {
                _trashBox = GetComponent<TrashBoxInteractable>();
            }
        }

        private void OnEnable()
        {
            if (_trashBox == null)
            {
                return;
            }

            _trashBox.ItemTrashed += HandleItemTrashed;
        }

        private void OnDisable()
        {
            if (_trashBox == null)
            {
                return;
            }

            _trashBox.ItemTrashed -= HandleItemTrashed;
        }

        private void HandleItemTrashed()
        {
            PlaySmoke();
        }

        private void PlaySmoke()
        {
            if (_smokePrefab == null)
            {
                return;
            }

            Transform spawnTransform = _spawnPoint != null ? _spawnPoint : transform;
            ParticleSystem smokeInstance = Instantiate(
                _smokePrefab,
                spawnTransform.position,
                _smokePrefab.transform.rotation);

            FxHelper.Play(smokeInstance);
            Destroy(smokeInstance.gameObject, _destroyDelay);
        }
    }
}
