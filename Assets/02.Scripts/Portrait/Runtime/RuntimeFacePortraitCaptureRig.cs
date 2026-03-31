using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RuntimeFacePortraitCaptureRig : MonoBehaviour
{
    private static readonly CustomizingType[] CachedCustomizingTypes =
        (CustomizingType[])Enum.GetValues(typeof(CustomizingType));

    private Camera _captureCamera;
    private Light _mainLight;
    private RenderTexture _renderTexture;
    private GameObject _characterInstance;
    private CustomizingCharacterView _characterView;
    private Renderer[] _characterRenderers = Array.Empty<Renderer>();
    private bool _isViewInitialized;

    public void Initialize(GameObject characterPrefab)
    {
        CreateCamera();
        CreateLights();
        CreateCharacter(characterPrefab);
    }

    public async UniTask ApplyAppearanceAsync(
        PlayerAppearanceSnapshot snapshot,
        ICustomizingManager customizingManager,
        ICustomizingAssetLoader assetLoader,
        CancellationToken cancellationToken = default)
    {
        if (snapshot == null || customizingManager == null || _characterView == null || assetLoader == null)
        {
            return;
        }

        if (!_isViewInitialized)
        {
            _characterView.Initialize(assetLoader);
            _isViewInitialized = true;
        }

        ApplyBaseEquipment(customizingManager);

        foreach (CustomizingType type in CachedCustomizingTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!snapshot.EquippedItemIds.TryGetValue(type, out string itemId) || string.IsNullOrEmpty(itemId))
            {
                _characterView.ClearSlot(type);
                continue;
            }

            CustomizingItemSO item = customizingManager.GetItemById(itemId);
            if (item == null)
            {
                _characterView.ClearSlot(type);
                continue;
            }

            await _characterView.ApplyItemAsync(type, item);
        }

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
    }

    public Sprite Capture(FacePortraitCaptureRequest request)
    {
        if (_captureCamera == null || request.Profile == null || _characterInstance == null)
        {
            return null;
        }

        EnsureRenderTexture(request.Resolution);
        _characterInstance.transform.localRotation = Quaternion.Euler(request.Profile.CharacterEulerAngles);

        Transform faceAnchor = FindFaceAnchor(request.Profile.FaceAnchorName);
        if (faceAnchor == null)
        {
            Debug.LogError($"[RuntimeFacePortraitCaptureRig] Face anchor '{request.Profile.FaceAnchorName}' was not found on capture character.");
            return null;
        }

        SetLayerRecursive(_characterInstance, request.CaptureLayer);
        _captureCamera.cullingMask = 1 << request.CaptureLayer;
        ApplyLighting(request);

        FacePortraitCameraController.Apply(_captureCamera, faceAnchor, request.Profile);

        SetRenderersEnabled(true);
        SetLightEnabled(true);
        try
        {
            _captureCamera.Render();
        }
        finally
        {
            SetLightEnabled(false);
            SetRenderersEnabled(false);
        }

        RenderTexture previousRenderTexture = RenderTexture.active;
        RenderTexture.active = _renderTexture;

        Texture2D texture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0f, 0f, _renderTexture.width, _renderTexture.height), 0, 0);
        texture.Apply();

        RenderTexture.active = previousRenderTexture;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
        sprite.name = $"FacePortrait_{request.Snapshot.ActorNumber}";

        return sprite;
    }

    public void DisposeRig()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
        }

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    private void CreateCamera()
    {
        GameObject cameraObject = new GameObject("FacePortraitCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetParent(transform, false);

        _captureCamera = cameraObject.AddComponent<Camera>();
        _captureCamera.enabled = false;
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;
        _captureCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _captureCamera.nearClipPlane = 0.01f;
        _captureCamera.farClipPlane = 10f;
        _captureCamera.allowHDR = true;
        _captureCamera.allowMSAA = false;
    }

    private void CreateLights()
    {
        GameObject lightObject = new GameObject("FacePortraitMainLight");
        lightObject.hideFlags = HideFlags.HideAndDontSave;
        lightObject.transform.SetParent(transform, false);

        _mainLight = lightObject.AddComponent<Light>();
        _mainLight.type = LightType.Directional;
        _mainLight.shadows = LightShadows.None;
        _mainLight.renderMode = LightRenderMode.ForcePixel;
        _mainLight.enabled = false;
    }

    private void CreateCharacter(GameObject characterPrefab)
    {
        _characterInstance = Instantiate(characterPrefab, transform);
        _characterInstance.hideFlags = HideFlags.HideAndDontSave;
        _characterInstance.name = $"Portrait_{characterPrefab.name}";
        _characterInstance.transform.localPosition = Vector3.zero;
        _characterInstance.transform.localRotation = Quaternion.identity;
        _characterInstance.transform.localScale = Vector3.one;

        foreach (Behaviour behaviour in _characterInstance.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour is Animator || behaviour is CustomizingCharacterView)
            {
                continue;
            }

            behaviour.enabled = false;
        }

        foreach (Collider collider in _characterInstance.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Rigidbody rigidbody in _characterInstance.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
        }

        _characterView = _characterInstance.GetComponent<CustomizingCharacterView>();
        _characterRenderers = _characterInstance.GetComponentsInChildren<Renderer>(true);
        SetRenderersEnabled(false);
    }

    private void ApplyBaseEquipment(ICustomizingManager customizingManager)
    {
        foreach ((BaseEquipmentType type, BaseEquipmentItemSO item) in customizingManager.GetAllBaseEquipmentItems())
        {
            _characterView.ApplyBaseEquipment(type, item);
        }
    }

    private void EnsureRenderTexture(int resolution)
    {
        if (_renderTexture != null && _renderTexture.width == resolution && _renderTexture.height == resolution)
        {
            return;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();

            if (Application.isPlaying)
            {
                Destroy(_renderTexture);
            }
            else
            {
                DestroyImmediate(_renderTexture);
            }
        }

        _renderTexture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _renderTexture.Create();
        _captureCamera.targetTexture = _renderTexture;
    }

    private Transform FindFaceAnchor(string faceAnchorName)
    {
        if (_characterInstance == null || string.IsNullOrWhiteSpace(faceAnchorName))
        {
            return null;
        }

        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(_characterInstance.transform);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            if (current.name == faceAnchorName)
            {
                return current;
            }

            foreach (Transform child in current)
            {
                queue.Enqueue(child);
            }
        }

        return null;
    }

    private static void SetLayerRecursive(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    private void ApplyLighting(FacePortraitCaptureRequest request)
    {
        if (_mainLight == null || request.Profile == null)
        {
            return;
        }

        _mainLight.cullingMask = 1 << request.CaptureLayer;
        _mainLight.color = request.Profile.MainLightColor;
        _mainLight.intensity = request.Profile.MainLightIntensity;
        _mainLight.transform.rotation = Quaternion.Euler(request.Profile.MainLightEulerAngles);
    }

    private void SetRenderersEnabled(bool isEnabled)
    {
        if (_characterRenderers == null)
        {
            return;
        }

        for (int i = 0; i < _characterRenderers.Length; i++)
        {
            if (_characterRenderers[i] != null)
            {
                _characterRenderers[i].enabled = isEnabled;
            }
        }
    }

    private void SetLightEnabled(bool isEnabled)
    {
        if (_mainLight != null)
        {
            _mainLight.enabled = isEnabled;
        }
    }
}
