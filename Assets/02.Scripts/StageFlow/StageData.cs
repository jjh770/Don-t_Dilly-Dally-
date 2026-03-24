using System;
using System.Collections.Generic;
using DontDillyDally.Data;

namespace DontDillyDally.StageFlow
{
    [Serializable]
    public class StageData
    {
        public List<DiseaseData> Patients = new();
        public float TotalTimeLimitSec = 600f;
        public float InitialPatientHealth = 100f;
        public float PatientHealthDrainPerSecond = 0.25f;
        public int PatientCount = 5;
        public int Difficulty = 1;
    }
}
