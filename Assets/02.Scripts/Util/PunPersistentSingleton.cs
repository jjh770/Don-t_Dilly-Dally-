using Photon.Pun;
using UnityEngine;

public class PunPersistentSingleton<T> : PunSingleton<T> where T : MonoBehaviourPunCallbacks
{
    private static T instance;
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}