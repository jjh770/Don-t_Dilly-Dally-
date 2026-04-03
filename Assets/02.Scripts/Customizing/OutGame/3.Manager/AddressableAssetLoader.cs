using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableAssetLoader : ICustomizingAssetLoader, IDisposable
{
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new();
    private readonly Dictionary<string, GameObject> _cachedAssets = new();

    public async UniTask<GameObject> LoadAsync(string addressableKey)
    {
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogWarning("[AddressableAssetLoader] addressableKey가 비어있습니다.");
            return null;
        }

        if (_cachedAssets.TryGetValue(addressableKey, out var cached))
        {
            return cached;
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _handles[addressableKey] = handle;
                _cachedAssets[addressableKey] = handle.Result;
                return handle.Result;
            }

            Debug.LogError($"[AddressableAssetLoader] 로드 실패: {addressableKey}");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AddressableAssetLoader] 로드 예외: {addressableKey}, {e.Message}");
            return null;
        }
    }

    public void Release(string addressableKey)
    {
        if (string.IsNullOrEmpty(addressableKey)) return;

        if (_handles.TryGetValue(addressableKey, out var handle))
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            _handles.Remove(addressableKey);
            _cachedAssets.Remove(addressableKey);
        }
    }

    public void ReleaseAll()
    {
        foreach (var kvp in _handles)
        {
            if (kvp.Value.IsValid())
            {
                Addressables.Release(kvp.Value);
            }
        }
        _handles.Clear();
        _cachedAssets.Clear();
    }

    public bool IsLoaded(string addressableKey)
    {
        return _cachedAssets.ContainsKey(addressableKey);
    }

    public async UniTask PreloadAsync(IEnumerable<string> addressableKeys)
    {
        var tasks = new List<UniTask>();

        foreach (var key in addressableKeys)
        {
            if (string.IsNullOrEmpty(key)) continue;
            if (_cachedAssets.ContainsKey(key)) continue;

            tasks.Add(LoadAsync(key));
        }

        if (tasks.Count > 0)
        {
            await UniTask.WhenAll(tasks);
        }
    }

    public void Dispose()
    {
        ReleaseAll();
    }
}
