using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Photon.Realtime;
using UnityEngine;

public sealed class PlayerPortraitService : IPlayerPortraitService
{
    private readonly IPlayerAppearanceSource _appearanceSource;
    private readonly RuntimeFacePortraitRenderer _renderer;
    private readonly FacePortraitProfile _profile;
    private readonly int _resolution;
    private readonly int _captureLayer;
    private readonly Dictionary<string, Sprite> _cachedSprites = new();
    private readonly Dictionary<int, string> _actorCacheKeys = new();
    private readonly Dictionary<string, Task<Sprite>> _pendingTasks = new();

    public PlayerPortraitService(
        IPlayerAppearanceSource appearanceSource,
        RuntimeFacePortraitRenderer renderer,
        FacePortraitProfile profile,
        int resolution,
        int captureLayer)
    {
        _appearanceSource = appearanceSource;
        _renderer = renderer;
        _profile = profile;
        _resolution = resolution;
        _captureLayer = captureLayer;
    }

    public PlayerAppearanceSnapshot CreateSnapshot(Player player)
    {
        return _appearanceSource?.Create(player);
    }

    public UniTask<Sprite> GetOrCreateAsync(PlayerAppearanceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot == null)
        {
            return UniTask.FromResult<Sprite>(null);
        }

        string cacheKey = snapshot.CreateCacheKey();
        ReplaceActorCache(snapshot.ActorNumber, cacheKey);

        if (_cachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return UniTask.FromResult(cachedSprite);
        }

        if (_pendingTasks.TryGetValue(cacheKey, out Task<Sprite> pendingTask))
        {
            return pendingTask.AsUniTask();
        }

        Task<Sprite> newTask = CreatePortraitTaskAsync(snapshot, cacheKey, cancellationToken);
        _pendingTasks[cacheKey] = newTask;
        return newTask.AsUniTask();
    }

    public bool TryGetCached(PlayerAppearanceSnapshot snapshot, out Sprite sprite)
    {
        sprite = null;

        if (snapshot == null)
        {
            return false;
        }

        string cacheKey = snapshot.CreateCacheKey();
        if (!_cachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite) || cachedSprite == null)
        {
            return false;
        }

        ReplaceActorCache(snapshot.ActorNumber, cacheKey);
        sprite = cachedSprite;
        return true;
    }

    public void Invalidate(int actorNumber)
    {
        if (!_actorCacheKeys.TryGetValue(actorNumber, out string cacheKey))
        {
            return;
        }

        _actorCacheKeys.Remove(actorNumber);
        RemoveCacheEntry(cacheKey);
    }

    public void ClearRoomCache()
    {
        foreach (Sprite sprite in _cachedSprites.Values)
        {
            DestroySprite(sprite);
        }

        _cachedSprites.Clear();
        _actorCacheKeys.Clear();
        _pendingTasks.Clear();
        _renderer?.Dispose();
    }

    private async Task<Sprite> CreatePortraitTaskAsync(PlayerAppearanceSnapshot snapshot, string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            if (_renderer == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            FacePortraitCaptureRequest request = new FacePortraitCaptureRequest(snapshot, _profile, _resolution, _captureLayer);
            Sprite sprite = await _renderer.RenderAsync(request, cancellationToken);

            if (sprite == null)
            {
                return null;
            }

            if (_actorCacheKeys.TryGetValue(snapshot.ActorNumber, out string currentKey) && currentKey == cacheKey)
            {
                _cachedSprites[cacheKey] = sprite;
                return sprite;
            }

            DestroySprite(sprite);
            return null;
        }
        finally
        {
            _pendingTasks.Remove(cacheKey);
        }
    }

    private void ReplaceActorCache(int actorNumber, string cacheKey)
    {
        if (_actorCacheKeys.TryGetValue(actorNumber, out string previousKey) && previousKey != cacheKey)
        {
            RemoveCacheEntry(previousKey);
        }

        _actorCacheKeys[actorNumber] = cacheKey;
    }

    private void RemoveCacheEntry(string cacheKey)
    {
        if (_cachedSprites.TryGetValue(cacheKey, out Sprite sprite))
        {
            DestroySprite(sprite);
            _cachedSprites.Remove(cacheKey);
        }

        _pendingTasks.Remove(cacheKey);
    }

    private static void DestroySprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Texture2D texture = sprite.texture;

        if (Application.isPlaying)
        {
            Object.Destroy(sprite);
            if (texture != null)
            {
                Object.Destroy(texture);
            }
        }
        else
        {
            Object.DestroyImmediate(sprite);
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
