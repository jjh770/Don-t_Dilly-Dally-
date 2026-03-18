using Cysharp.Threading.Tasks;

public interface IPlayerDataRepository 
{
    public UniTask Save(PlayerSaveData saveData);

    public UniTask<PlayerSaveData> Load();
}
