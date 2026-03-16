using Photon.Pun;
using UnityEngine;

public class PunPersistentSingleton<T> : PunSingleton<T> where T : MonoBehaviourPunCallbacks
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}