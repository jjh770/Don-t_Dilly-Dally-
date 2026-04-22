using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [CreateAssetMenu(fileName = "StageDefinition", menuName = "DontDillyDally/Stage Flow/Stage Definition")]
    public class StageDefinitionSO : ScriptableObject
    {
        [Header("Stage Info")]
        [SerializeField] private string _stageId = string.Empty;
        [SerializeField] private string _stageName = string.Empty;
        [SerializeField] private Sprite _stageThumbnail;
        [TextArea(minLines: 2, maxLines: 5)]
        [SerializeField] private string _description;
        [SerializeField] private GameObject _stagePrefab;

        [Header("Stage Settings")]
        [SerializeField] private StageSettings _settings = new();

        [Header("Unlock Condition")]
        [SerializeField] private int _requiredHospitalLevel = 0;

        [Header("Patient Appearance")]
        [Tooltip("이 스테이지에서만 등장하는 환자 외형 프리셋. 공통 풀과 합쳐져 랜덤 추첨됩니다.")]
        [SerializeField] private List<PatientAppearanceSO> _stageOnlyAppearances = new();

        public string StageId => _stageId;
        public string StageName => string.IsNullOrWhiteSpace(_stageName) ? _stageId : _stageName;
        public Sprite StageThumbnail => _stageThumbnail;
        public string Description => _description;
        public StageSettings Settings => _settings;
        public int RequiredHospitalLevel => _requiredHospitalLevel;
        public bool IsDefaultUnlocked => _requiredHospitalLevel == 0;

        public GameObject StagePrefab => _stagePrefab;
        public IReadOnlyList<PatientAppearanceSO> StageOnlyAppearances => _stageOnlyAppearances;

        private void OnEnable()
        {
            EnsureSettingsInitialized();
        }

        private void OnValidate()
        {
            EnsureSettingsInitialized();
        }

        public StageRuntimeData CreateRuntimeData()
        {
            return new StageRuntimeData
            {
                StageId = _stageId,
                Settings = new StageSettings(_settings)
            };
        }

        private void EnsureSettingsInitialized()
        {
            _settings ??= new StageSettings();
            _settings.PatientSettings ??= new StagePatientSettings();
            _settings.MiniGameSettings ??= new StageMiniGameSettings();
            _settings.EmergencySettings ??= new StageEmergencySettings();
        }
    }
}
