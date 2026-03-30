using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class RuntimeFacePortraitRenderer : IDisposable
{
    private readonly GameObject _characterPrefab;
    private readonly ICustomizingManager _customizingManager;
    private readonly ICustomizingAssetLoader _assetLoader;
    private RuntimeFacePortraitCaptureRig _rig;

    public RuntimeFacePortraitRenderer(GameObject characterPrefab, ICustomizingManager customizingManager)
    {
        _characterPrefab = characterPrefab;
        _customizingManager = customizingManager;
        _assetLoader = new AddressableAssetLoader();
    }

    public async UniTask<Sprite> RenderAsync(FacePortraitCaptureRequest request)
    {
        if (_characterPrefab == null)
        {
            Debug.LogWarning("[RuntimeFacePortraitRenderer] Portrait character prefab is not assigned.");
            return null;
        }

        if (_customizingManager == null || !_customizingManager.IsInitialized)
        {
            Debug.LogWarning("[RuntimeFacePortraitRenderer] CustomizingManager is not ready. Portrait capture skipped.");
            return null;
        }

        EnsureRig();
        if (_rig == null)
        {
            return null;
        }

        await _rig.ApplyAppearanceAsync(request.Snapshot, _customizingManager, _assetLoader);
        return _rig.Capture(request);
    }

    public void Dispose()
    {
        if (_rig != null)
        {
            _rig.DisposeRig();
            _rig = null;
        }

        (_assetLoader as IDisposable)?.Dispose();
    }

    private void EnsureRig()
    {
        if (_rig != null)
        {
            return;
        }

        GameObject rigRoot = new GameObject("RuntimeFacePortraitCaptureRig");
        rigRoot.hideFlags = HideFlags.HideAndDontSave;
        rigRoot.transform.position = new Vector3(10000f, -10000f, 10000f);
        _rig = rigRoot.AddComponent<RuntimeFacePortraitCaptureRig>();
        _rig.Initialize(_characterPrefab);
    }
}
