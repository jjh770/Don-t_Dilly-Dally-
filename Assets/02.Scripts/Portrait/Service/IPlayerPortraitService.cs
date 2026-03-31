using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IPlayerPortraitService
{
    UniTask<Sprite> GetOrCreateAsync(PlayerAppearanceSnapshot snapshot, CancellationToken cancellationToken = default);
    bool TryGetCached(PlayerAppearanceSnapshot snapshot, out Sprite sprite);
    void Invalidate(int actorNumber);
    void ClearRoomCache();
}
