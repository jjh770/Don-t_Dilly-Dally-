using Cysharp.Threading.Tasks;

public class PlayerDataFirebaseRepository : IPlayerDataRepository
{
    public async UniTask<PlayerSaveData> Load()
    {
        await UniTask.Yield(); 
        return null;
    }

    public async UniTask Save(PlayerSaveData saveData)
    {
        await UniTask.Yield();
    }
}
