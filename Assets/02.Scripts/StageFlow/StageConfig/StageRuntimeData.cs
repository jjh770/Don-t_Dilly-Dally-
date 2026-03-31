using DontDillyDally.Data;
using System;
using System.Collections.Generic;

namespace DontDillyDally.StageFlow
{
    [Serializable]
    public class StageRuntimeData
    {
        public string StageId = string.Empty;
        public List<DiseaseData> Patients = new();
        public float TotalTimeLimitSec = 600f;
        public float MaxPatientHealth = 100f;
        public float PatientHealthDrainPerSecond = 0.25f;
        public int PatientCount = 5;
        public int Difficulty = 1;
        public int SavedCount = 0;
    }
}
