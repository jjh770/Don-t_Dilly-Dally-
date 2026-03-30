using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Firebase;


public class FirebaseInitializer : PersistentSingleton<FirebaseInitializer>
{
    private Firebase.FirebaseApp _app = null;
    public FirebaseFirestore Database { get; private set; }

    public static event Action OnFirebaseInitialized;

    public bool IsFirebaseInitialized { get; private set; }
    protected override void Awake()
    {
        base.Awake();
        IsFirebaseInitialized = false;

        if (Instance != this) return;

        FirebaseInit().Forget(e => UnityEngine.Debug.LogException(e));
    }

    private async UniTask FirebaseInit()
    {

        var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();

        if (status == Firebase.DependencyStatus.Available)
        {
            // 1. Firebase 초기화 성공했다면.
            _app = Firebase.FirebaseApp.DefaultInstance; // FirebaseApp 모듈 가져오기.

            Database = FirebaseFirestore.DefaultInstance; // Firestore 모듈 가져오기.

            #if UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
            // 클론 에디터에서만 오프라인 캐시를 꺼서 충돌(Lock)을 방지합니다.
            Database.Settings.PersistenceEnabled = false;
            }
            #endif

            Debug.Log("[FirebaseInitializer] Firebase 초기화 성공");
            Debug.Log(FirebaseApp.DefaultInstance.Options.ProjectId);
            Debug.Log(FirebaseApp.DefaultInstance.Options.StorageBucket);

            OnFirebaseInitialized?.Invoke();
            IsFirebaseInitialized = true;
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {status}");
        }
    }
}
