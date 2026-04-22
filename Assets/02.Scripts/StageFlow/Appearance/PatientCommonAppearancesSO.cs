using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [CreateAssetMenu(
        fileName = "PatientCommonAppearances",
        menuName = "DontDillyDally/Patient Appearance/Common Pool")]
    public class PatientCommonAppearancesSO : ScriptableObject
    {
        [Tooltip("모든 스테이지에서 공통으로 후보가 되는 환자 외형 프리셋들.")]
        [SerializeField] private List<PatientAppearanceSO> _appearances = new();

        public IReadOnlyList<PatientAppearanceSO> Appearances => _appearances;
    }
}
