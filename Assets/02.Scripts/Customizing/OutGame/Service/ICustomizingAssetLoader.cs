using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ICustomizingAssetLoader
{
    UniTask<GameObject> LoadAsync(string addressableKey);
    UniTask PreloadAsync(IEnumerable<string> addressableKeys);
    void Release(string addressableKey);
    void ReleaseAll();
    bool IsLoaded(string addressableKey);
}
