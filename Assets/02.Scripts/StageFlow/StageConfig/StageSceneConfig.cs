using System.Collections.Generic;
using DontDillyDally.Data;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    // 스테이지 프리팹의 루트에 배치되어 스테이지별 모든 설정을 통합 관리하는 컴포넌트
    // 각 스테이지 프리팹에 하나씩 배치
    public class StageSceneConfig : MonoBehaviour
    {
        public static StageSceneConfig Instance { get; private set; }

        [Header("스테이지 식별")]
        [Tooltip("StageDefinitionSO의 StageId와 일치해야 합니다")]
        [SerializeField] private string _stageId;

        [Header("플레이어 스폰 포인트")]
        [SerializeField] private Transform _surgeonSpawnPoint;
        [SerializeField] private Transform[] _assistantSpawnPoints;

        [Header("플레이어 리스폰 포인트")]
        [SerializeField] private Transform _surgeonRespawnPoint;
        [SerializeField] private Transform[] _assistantRespawnPoints;

        [Header("아이템 스폰 설정")]
        [Tooltip("이 스테이지에서 사용할 아이템 스폰 카탈로그")]
        [SerializeField] private SceneItemSpawnCatalog _itemSpawnCatalog;
        [SerializeField] private List<Transform> _itemSpawnPoints = new List<Transform>();
        [SerializeField] private List<Transform> _traySpawnPoints = new List<Transform>();

        [Header("Area Spawn (선택)")]
        [Tooltip("포인트 대신 영역 내 랜덤 스폰을 사용할 경우")]
        [SerializeField] private Collider _spawnArea;

        // 스테이지 식별
        public string StageId => _stageId;

        // 플레이어 스폰
        public Transform SurgeonSpawnPoint => _surgeonSpawnPoint;
        public Transform[] AssistantSpawnPoints => _assistantSpawnPoints;
        public Collider SpawnArea => _spawnArea;

        // 플레이어 리스폰
        public Transform SurgeonRespawnPoint => _surgeonRespawnPoint;
        public Transform[] AssistantRespawnPoints => _assistantRespawnPoints;

        // 아이템 스폰
        public SceneItemSpawnCatalog ItemSpawnCatalog => _itemSpawnCatalog;
        public List<Transform> ItemSpawnPoints => _itemSpawnPoints;
        public List<Transform> TraySpawnPoints => _traySpawnPoints;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[StageSceneConfig] 이미 인스턴스가 존재합니다. 중복 인스턴스를 파괴합니다.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Transform[] GetSpawnPointsByRole(RoleType role)
        {
            if (role == RoleType.Surgeon && _surgeonSpawnPoint != null)
            {
                return new[] { _surgeonSpawnPoint };
            }

            return _assistantSpawnPoints;
        }

        public Transform GetRespawnPointByRole(RoleType role)
        {
            if (role == RoleType.Surgeon && _surgeonRespawnPoint != null)
            {
                return _surgeonRespawnPoint;
            }

            if (_assistantRespawnPoints != null && _assistantRespawnPoints.Length > 0)
            {
                return _assistantRespawnPoints[Random.Range(0, _assistantRespawnPoints.Length)];
            }

            return null;
        }

        public Transform GetAvailableRespawnPoint(RoleType role, System.Func<Vector3, bool> isOccupied)
        {
            if (role == RoleType.Surgeon && _surgeonRespawnPoint != null)
            {
                return _surgeonRespawnPoint;
            }

            if (_assistantRespawnPoints == null || _assistantRespawnPoints.Length == 0)
            {
                return null;
            }

            foreach (Transform point in _assistantRespawnPoints)
            {
                if (!isOccupied(point.position))
                {
                    return point;
                }
            }

            return _assistantRespawnPoints[Random.Range(0, _assistantRespawnPoints.Length)];
        }
    }
}
