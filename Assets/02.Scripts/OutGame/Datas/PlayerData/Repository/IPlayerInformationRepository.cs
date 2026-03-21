using Cysharp.Threading.Tasks;

public interface IPlayerInformationRepository 
{
    public UniTask Save(string account, PlayerInformation saveData);

    public UniTask<PlayerInformation> Load(string account);

}
