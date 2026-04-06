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
        public StageSettings Settings = new();
        public int SavedCount = 0;
    }
}
