using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [CreateAssetMenu(fileName = "StageDefinition", menuName = "DontDillyDally/Stage Flow/Stage Definition")]
    public class StageDefinitionSO : ScriptableObject
    {
        [Header("스테이지 설정")]
        [SerializeField] private string _stageId = string.Empty;
        [SerializeField] private int _patientCount = 5;
        [SerializeField] private float _totalTimeLimitSec = 600f;
        [SerializeField] private float _initialPatientHealth = 100f;
        [SerializeField] private float _patientHealthDrainPerSecond = 0.25f;
        [SerializeField] private int _difficulty = 1;

        public string StageId => _stageId;
        public int PatientCount => _patientCount;
        public float TotalTimeLimitSec => _totalTimeLimitSec;
        public float InitialPatientHealth => _initialPatientHealth;
        public float PatientHealthDrainPerSecond => _patientHealthDrainPerSecond;
        public int Difficulty => _difficulty;

        // SO는 원본 설정만 들고 있고, 실제 플레이에는 별도의 런타임 데이터를 생성해서 넘깁니다.
        public StageRuntimeData CreateRuntimeData()
        {
            return new StageRuntimeData
            {
                StageId = _stageId,
                PatientCount = _patientCount,
                TotalTimeLimitSec = _totalTimeLimitSec,
                MaxPatientHealth = _initialPatientHealth,
                PatientHealthDrainPerSecond = _patientHealthDrainPerSecond,
                Difficulty = _difficulty
            };
        }
    }
}
