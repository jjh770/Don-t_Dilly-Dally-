using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;


public class FirebaseInitializer : PersistentSingleton<FirebaseInitializer>
{
    private Firebase.FirebaseApp _app = null;
    public FirebaseFirestore Database { get; private set; }
    public bool IsInitialized => _app != null && Database != null;

    public static event Action OnFirebaseInitialized;

    protected override void Awake()
    {
        base.Awake();
        FirebaseInit().Forget();
    }

    private async UniTask FirebaseInit()
    {

        var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();

        if (status == Firebase.DependencyStatus.Available)
        {
            // 1. Firebase 초기화 성공했다면.
            _app = Firebase.FirebaseApp.DefaultInstance; // FirebaseApp 모듈 가져오기.

            Database = FirebaseFirestore.DefaultInstance; // Firestore 모듈 가져오기.

            Debug.Log("Firebase 초기화 성공");
            OnFirebaseInitialized?.Invoke();
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {status}");
        }
    }

}