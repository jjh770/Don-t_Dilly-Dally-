using Cysharp.Threading.Tasks;

public interface IRepository<T>
{
    UniTask Save(T data);
    UniTask<T> Load();
}
