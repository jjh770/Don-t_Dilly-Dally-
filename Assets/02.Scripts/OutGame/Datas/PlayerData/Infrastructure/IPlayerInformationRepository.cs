using Cysharp.Threading.Tasks;

public interface IPlayerInformationRepository 
{
    public UniTask Save(PlayerInformation saveData);

    public UniTask<PlayerInformation> Load();

}
