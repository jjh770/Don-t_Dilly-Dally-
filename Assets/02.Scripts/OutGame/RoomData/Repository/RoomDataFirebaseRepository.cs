using Cysharp.Threading.Tasks;
using UnityEngine;

public class RoomDataFirebaseRepository : IRoomDataRepository
{
    public async UniTask<RoomSaveData> Load(string roomCode)
    {
        await UniTask.Yield();
        return null;
    }

    public async UniTask Save(string roomCode, RoomSaveData saveData)
    {
        await UniTask.Yield();
    }
}
