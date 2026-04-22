using System.Collections.Generic;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public class PatientAppearanceVariator : MonoBehaviour
    {
        [Header("Slot Renderers")]
        [SerializeField] private SkinnedMeshRenderer _earsRenderer;
        [SerializeField] private SkinnedMeshRenderer _faceRenderer;
        [SerializeField] private SkinnedMeshRenderer _hairRenderer;
        [SerializeField] private SkinnedMeshRenderer _bodyRenderer;
        [SerializeField] private SkinnedMeshRenderer _outfitRenderer;
        [SerializeField] private SkinnedMeshRenderer _shortsRenderer;
        [SerializeField] private SkinnedMeshRenderer _socksRenderer;
        [SerializeField] private SkinnedMeshRenderer _shoesRenderer;

        [Header("Appearance Pool")]
        [Tooltip("모든 스테이지에서 후보가 되는 공통 외형 풀.")]
        [SerializeField] private PatientCommonAppearancesSO _commonAppearances;

        private Material _earsOriginalMaterial;
        private Material _faceOriginalMaterial;
        private Material _hairOriginalMaterial;
        private Material _bodyOriginalMaterial;
        private Material _outfitOriginalMaterial;
        private Material _shortsOriginalMaterial;
        private Material _socksOriginalMaterial;
        private Material _shoesOriginalMaterial;
        private bool _materialsCached;

        private void OnEnable()
        {
            ApplyRandomAppearance();
        }

        public void ApplyRandomAppearance()
        {
            EnsureOriginalMaterialsCached();

            List<PatientAppearanceSO> pool = BuildPool();
            if (pool.Count == 0)
            {
                return;
            }

            int seed = ResolveSeed();
            int index = new System.Random(seed).Next(0, pool.Count);
            ApplyAppearance(pool[index]);
        }

        private void EnsureOriginalMaterialsCached()
        {
            if (_materialsCached)
            {
                return;
            }

            _earsOriginalMaterial = _earsRenderer != null ? _earsRenderer.sharedMaterial : null;
            _faceOriginalMaterial = _faceRenderer != null ? _faceRenderer.sharedMaterial : null;
            _hairOriginalMaterial = _hairRenderer != null ? _hairRenderer.sharedMaterial : null;
            _bodyOriginalMaterial = _bodyRenderer != null ? _bodyRenderer.sharedMaterial : null;
            _outfitOriginalMaterial = _outfitRenderer != null ? _outfitRenderer.sharedMaterial : null;
            _shortsOriginalMaterial = _shortsRenderer != null ? _shortsRenderer.sharedMaterial : null;
            _socksOriginalMaterial = _socksRenderer != null ? _socksRenderer.sharedMaterial : null;
            _shoesOriginalMaterial = _shoesRenderer != null ? _shoesRenderer.sharedMaterial : null;
            _materialsCached = true;
        }

        private List<PatientAppearanceSO> BuildPool()
        {
            List<PatientAppearanceSO> pool = new();

            if (_commonAppearances != null)
            {
                foreach (PatientAppearanceSO preset in _commonAppearances.Appearances)
                {
                    if (preset != null)
                    {
                        pool.Add(preset);
                    }
                }
            }

            StageDefinitionSO stage = ResolveCurrentStageDefinition();
            if (stage != null)
            {
                foreach (PatientAppearanceSO preset in stage.StageOnlyAppearances)
                {
                    if (preset != null)
                    {
                        pool.Add(preset);
                    }
                }
            }

            return pool;
        }

        private static StageDefinitionSO ResolveCurrentStageDefinition()
        {
            return RoomDataManager.Instance != null
                ? RoomDataManager.Instance.CurrentStageDefinition
                : null;
        }

        private static int ResolveSeed()
        {
            if (StageFlowManager.Instance != null)
            {
                return StageFlowManager.Instance.DirectionSeed;
            }
            return UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        private void ApplyAppearance(PatientAppearanceSO preset)
        {
            ApplySlot(_earsRenderer, preset.Ears, _earsOriginalMaterial);
            ApplySlot(_faceRenderer, preset.Face, _faceOriginalMaterial);
            ApplySlot(_hairRenderer, preset.Hair, _hairOriginalMaterial);
            ApplySlot(_bodyRenderer, preset.Body, _bodyOriginalMaterial);
            ApplySlot(_outfitRenderer, preset.Outfit, _outfitOriginalMaterial);
            ApplySlot(_shortsRenderer, preset.Shorts, _shortsOriginalMaterial);
            ApplySlot(_socksRenderer, preset.Socks, _socksOriginalMaterial);
            ApplySlot(_shoesRenderer, preset.Shoes, _shoesOriginalMaterial);
        }

        private static void ApplySlot(SkinnedMeshRenderer renderer, AppearanceSlot slot, Material originalMaterial)
        {
            if (renderer == null)
            {
                return;
            }

            if (slot.Mesh == null)
            {
                renderer.enabled = false;
                return;
            }

            renderer.enabled = true;
            renderer.sharedMesh = slot.Mesh;
            renderer.sharedMaterial = slot.MaterialOverride != null ? slot.MaterialOverride : originalMaterial;
        }
    }
}
