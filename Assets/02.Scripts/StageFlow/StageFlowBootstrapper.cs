using DontDillyDally.Data;
using DontDillyDally.MiniGame;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public class StageFlowBootstrapper : MonoBehaviour
    {
        [Header("매니저 참조")]
        [SerializeField] private StageFlowManager _stageFlowManager;
        [SerializeField] private StageFlowRpcHandler _rpcHandler;
        [SerializeField] private DiseaseGenerationManager _diseaseGenManager;
        [SerializeField] private MiniGameLauncher _miniGameLauncher;

        [Header("스테이지 설정")]
        [SerializeField] private int _patientCount = 5;
        [SerializeField] private float _totalTimeLimitSec = 600f;
        [SerializeField] private float _initialPatientHealth = 100f;
        [SerializeField] private float _patientHealthDrainPerSecond = 0.25f;
        [SerializeField] private int _difficulty = 1;

        private void Start()
        {
            var stageData = new StageData
            {
                PatientCount = _patientCount,
                TotalTimeLimitSec = _totalTimeLimitSec,
                InitialPatientHealth = _initialPatientHealth,
                PatientHealthDrainPerSecond = _patientHealthDrainPerSecond,
                Difficulty = _difficulty
            };

            _stageFlowManager.Initialize(_diseaseGenManager, _miniGameLauncher, stageData);
        }
    }
}
