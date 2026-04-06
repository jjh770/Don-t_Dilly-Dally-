using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    /// <summary>
    /// 스테이지별 아이템 스폰 설정을 담는 컴포넌트입니다.
    /// 씬에 스테이지마다 하나씩 배치하여 카탈로그와 스폰 포인트를 묶어 관리합니다.
    /// </summary>
    public class StageItemConfig : MonoBehaviour
    {
        [Header("스테이지 식별")]
        [Tooltip("이 설정이 적용될 스테이지 ID (StageDefinitionSO의 StageId와 일치해야 합니다)")]
        [SerializeField] private string _stageId;

        [Header("아이템 카탈로그")]
        [Tooltip("이 스테이지에서 사용할 아이템 스폰 카탈로그")]
        [SerializeField] private SceneItemSpawnCatalog _spawnCatalog;

        [Header("일반 아이템 배치 위치")]
        [Tooltip("일반 공급원을 생성할 위치 목록")]
        [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();

        [Header("트레이 배치 위치")]
        [Tooltip("트레이를 생성할 위치 목록")]
        [SerializeField] private List<Transform> _traySpawnPoints = new List<Transform>();

        public string StageId => _stageId;
        public SceneItemSpawnCatalog SpawnCatalog => _spawnCatalog;
        public List<Transform> SpawnPoints => _spawnPoints;
        public List<Transform> TraySpawnPoints => _traySpawnPoints;
    }
}
