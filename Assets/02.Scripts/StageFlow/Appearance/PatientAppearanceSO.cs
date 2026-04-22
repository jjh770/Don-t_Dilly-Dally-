using System;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [Serializable]
    public struct AppearanceSlot
    {
        [Tooltip("null이면 해당 부위 Renderer를 비활성화합니다.")]
        public Mesh Mesh;

        [Tooltip("null이면 프리팹의 원본 Material을 유지합니다.")]
        public Material MaterialOverride;
    }

    [CreateAssetMenu(
        fileName = "PatientAppearance",
        menuName = "DontDillyDally/Patient Appearance/Appearance Preset")]
    public class PatientAppearanceSO : ScriptableObject
    {
        [SerializeField] private string _presetId = string.Empty;

        [Header("Slots")]
        [SerializeField] private AppearanceSlot _ears;
        [SerializeField] private AppearanceSlot _face;
        [SerializeField] private AppearanceSlot _hair;
        [SerializeField] private AppearanceSlot _body;
        [SerializeField] private AppearanceSlot _outfit;
        [SerializeField] private AppearanceSlot _shorts;
        [SerializeField] private AppearanceSlot _socks;
        [SerializeField] private AppearanceSlot _shoes;

        public string PresetId => _presetId;

        public AppearanceSlot Ears => _ears;
        public AppearanceSlot Face => _face;
        public AppearanceSlot Hair => _hair;
        public AppearanceSlot Body => _body;
        public AppearanceSlot Outfit => _outfit;
        public AppearanceSlot Shorts => _shorts;
        public AppearanceSlot Socks => _socks;
        public AppearanceSlot Shoes => _shoes;
    }
}
