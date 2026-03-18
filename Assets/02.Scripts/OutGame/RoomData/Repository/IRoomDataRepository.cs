using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IRoomDataRepository 
{
    public UniTask Save(string roomCode, RoomSaveData saveData);

    public UniTask<RoomSaveData> Load(string roomCode);

    public UniTask<bool> IsExist(string roomCode);
}
