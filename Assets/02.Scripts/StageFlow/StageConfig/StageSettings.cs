using System;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [Serializable]
    public class StageEmergencySettings
    {
        [Range(0f, 1f)]
        public float RecipeFailTriggerChance = 0.7f;

        [Range(0f, 1f)]
        public float MiniGameFailTriggerChance = 0.7f;

        [Min(0f)]
        public float RandomCheckIntervalSec = 30f;

        [Range(0f, 1f)]
        public float RandomTriggerChance = 0.05f;

        public StageEmergencySettings()
        {
        }

        public StageEmergencySettings(StageEmergencySettings source)
        {
            if (source == null)
            {
                return;
            }

            RecipeFailTriggerChance = source.RecipeFailTriggerChance;
            MiniGameFailTriggerChance = source.MiniGameFailTriggerChance;
            RandomCheckIntervalSec = source.RandomCheckIntervalSec;
            RandomTriggerChance = source.RandomTriggerChance;
        }
    }

    [Serializable]
    public class StageSettings
    {
        [Min(1)]
        public int PatientCount = 5;

        [Min(0f)]
        public float TotalTimeLimitSec = 600f;

        [Min(0f)]
        public float InitialPatientHealth = 100f;

        [Min(0f)]
        public float PatientHealthDrainPerSecond = 0.25f;

        [Min(1)]
        public int Difficulty = 1;

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

            PatientCount = source.PatientCount;
            TotalTimeLimitSec = source.TotalTimeLimitSec;
            InitialPatientHealth = source.InitialPatientHealth;
            PatientHealthDrainPerSecond = source.PatientHealthDrainPerSecond;
            Difficulty = source.Difficulty;
            EmergencySettings = new StageEmergencySettings(source.EmergencySettings);
        }
    }
}
