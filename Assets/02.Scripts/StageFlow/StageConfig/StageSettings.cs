using System;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [Serializable]
    public class StagePatientSettings
    {
        [Min(1)]
        public int PatientCount = 5;

        [Min(0f)]
        public float InitialPatientHealth = 100f;

        [Min(0f)]
        public float PatientHealthDrainPerSecond = 0.25f;

        [Min(1)]
        public int Difficulty = 1;

        [Min(0f)]
        public float RecipeFailPenalty = 20f;

        public StagePatientSettings()
        {
        }

        public StagePatientSettings(StagePatientSettings source)
        {
            if (source == null)
            {
                return;
            }

            PatientCount = source.PatientCount;
            InitialPatientHealth = source.InitialPatientHealth;
            PatientHealthDrainPerSecond = source.PatientHealthDrainPerSecond;
            Difficulty = source.Difficulty;
            RecipeFailPenalty = source.RecipeFailPenalty;
        }
    }

    [Serializable]
    public class StageMiniGameSettings
    {
        [Min(0f)]
        public float FailPenalty = 10f;

        [Min(0f)]
        public float SuccessHeal = 5f;

        public StageMiniGameSettings()
        {
        }

        public StageMiniGameSettings(StageMiniGameSettings source)
        {
            if (source == null)
            {
                return;
            }

            FailPenalty = source.FailPenalty;
            SuccessHeal = source.SuccessHeal;
        }
    }

    [Serializable]
    public class StageEmergencySettings
    {
        [Min(0f)]
        public float TrayDurationSec = 20f;

        [Min(0f)]
        public float DiagnosisDurationSec = 15f;

        [Min(0f)]
        public float DiagnosisOperationDurationSec = 5f;

        [Range(0f, 1f)]
        public float TrayEventSelectionChance = 0.5f;

        [Range(0f, 1f)]
        public float RecipeFailTriggerChance = 0.7f;

        [Range(0f, 1f)]
        public float MiniGameFailTriggerChance = 0.7f;

        [Min(0f)]
        public float RandomCheckIntervalSec = 30f;

        [Range(0f, 1f)]
        public float RandomTriggerChance = 0.05f;

        [Min(0f)]
        public float EmergencyFailPenalty = 10f;

        public StageEmergencySettings()
        {
        }

        public StageEmergencySettings(StageEmergencySettings source)
        {
            if (source == null)
            {
                return;
            }

            TrayDurationSec = source.TrayDurationSec;
            DiagnosisDurationSec = source.DiagnosisDurationSec;
            DiagnosisOperationDurationSec = source.DiagnosisOperationDurationSec;
            TrayEventSelectionChance = source.TrayEventSelectionChance;
            RecipeFailTriggerChance = source.RecipeFailTriggerChance;
            MiniGameFailTriggerChance = source.MiniGameFailTriggerChance;
            RandomCheckIntervalSec = source.RandomCheckIntervalSec;
            RandomTriggerChance = source.RandomTriggerChance;
            EmergencyFailPenalty = source.EmergencyFailPenalty;
        }
    }

    [Serializable]
    public class StageSettings
    {
        [Min(0f)]
        public float TotalTimeLimitSec = 600f;

        public StagePatientSettings PatientSettings = new();
        public StageMiniGameSettings MiniGameSettings = new();
        public StageEmergencySettings EmergencySettings = new();

        public StageSettings()
        {
        }

        public StageSettings(StageSettings source)
        {
            if (source == null)
            {
                return;
            }

            TotalTimeLimitSec = source.TotalTimeLimitSec;
            PatientSettings = new StagePatientSettings(source.PatientSettings);
            MiniGameSettings = new StageMiniGameSettings(source.MiniGameSettings);
            EmergencySettings = new StageEmergencySettings(source.EmergencySettings);
        }
    }
}
