using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.Data
{
    [CreateAssetMenu(fileName = "EmergencyUiCatalog", menuName = "DontDillyDally/Emergency UI Catalog")]
    public class EmergencyUiCatalogSO : ScriptableObject
    {
        [Header("Shared")]
        [SerializeField] private MaterialIconTable _materialIconTable;

        [Header("Material Event")]
        [SerializeField] private List<TargetMaterialProcessEntry> _targetMaterialProcesses = new List<TargetMaterialProcessEntry>();

        [Header("Diagnosis Event")]
        [SerializeField] private List<DiagnosisEmergencyEntry> _diagnosisEntries = new List<DiagnosisEmergencyEntry>();

        private Dictionary<CraftedMaterialType, TargetMaterialProcessEntry> _targetProcessCache;
        private Dictionary<DiagnosisScanType, Sprite> _diagnosisCache;

        public MaterialIconTable MaterialIconTable => _materialIconTable;

        public Sprite GetDiagnosisIcon(DiagnosisScanType diagnosisType)
        {
            BuildDiagnosisCacheIfNeeded();

            if (_diagnosisCache.TryGetValue(diagnosisType, out Sprite icon))
            {
                return icon;
            }

            return null;
        }

        public bool TryGetTargetMaterialProcess(CraftedMaterialType targetMaterial, out TargetMaterialProcessEntry entry)
        {
            BuildTargetProcessCacheIfNeeded();
            return _targetProcessCache.TryGetValue(targetMaterial, out entry);
        }

        private void BuildTargetProcessCacheIfNeeded()
        {
            if (_targetProcessCache != null)
            {
                return;
            }

            _targetProcessCache = new Dictionary<CraftedMaterialType, TargetMaterialProcessEntry>(_targetMaterialProcesses.Count);
            for (int i = 0; i < _targetMaterialProcesses.Count; i++)
            {
                TargetMaterialProcessEntry entry = _targetMaterialProcesses[i];
                if (entry == null || entry.TargetMaterial == CraftedMaterialType.None)
                {
                    continue;
                }

                _targetProcessCache[entry.TargetMaterial] = entry;
            }
        }

        private void BuildDiagnosisCacheIfNeeded()
        {
            if (_diagnosisCache != null)
            {
                return;
            }

            _diagnosisCache = new Dictionary<DiagnosisScanType, Sprite>(_diagnosisEntries.Count);
            for (int i = 0; i < _diagnosisEntries.Count; i++)
            {
                DiagnosisEmergencyEntry entry = _diagnosisEntries[i];
                if (entry == null || entry.DiagnosisType == DiagnosisScanType.None || entry.Icon == null)
                {
                    continue;
                }

                _diagnosisCache[entry.DiagnosisType] = entry.Icon;
            }
        }

        private void OnValidate()
        {
            _targetProcessCache = null;
            _diagnosisCache = null;
        }

        [Serializable]
        public class TargetMaterialProcessEntry
        {
            public CraftedMaterialType TargetMaterial = CraftedMaterialType.None;
            public ActionType ProcessAction = ActionType.None;
        }

        [Serializable]
        public class DiagnosisEmergencyEntry
        {
            public DiagnosisScanType DiagnosisType = DiagnosisScanType.None;
            public Sprite Icon;
        }
    }
}
