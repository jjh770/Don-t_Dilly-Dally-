using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 로딩과 전환을 관리하는 매니저입니다.
/// </summary>
public class SceneLoadManager : PunPersistentSingleton<SceneLoadManager>
{
    [SerializeField]
    private SceneDataSO[] sceneDataSOs;

    private Dictionary<ESceneType, SceneDataSO> _sceneDataMap = new Dictionary<ESceneType, SceneDataSO>();

    #region Events
    public event Action<string> OnSceneLoadStart;
    public event Action<string> OnSceneLoadComplete;
    #endregion

    private bool _isLoading = false;
    private float _loadingProgress = 0f;
    private SceneDataSO _nextSceneData;


    public bool IsLoading => _isLoading;
    public float LoadingProgress => _loadingProgress;
    public SceneDataSO NextSceneData => _nextSceneData;

    protected override void Awake()
    {
        base.Awake();
    }
    private void Start()
    {
        foreach(SceneDataSO data in sceneDataSOs)
        {
            ESceneType type = data.SceneType;
            if (_sceneDataMap.ContainsKey(type))
            {
                Debug.LogWarning($"[SceneLoadManager] Duplicate scene type: {type}. Skipping.");
                continue;
            }
            _sceneDataMap[type] = data;
        }
    }


    #region Public Methods - Scene Loading

    public void BeginSceneLoad(ESceneType type)
    {
        if (_isLoading)
        {
            Debug.LogWarning($"[SceneLoadManager] Already loading: {_nextSceneData?.name}");
            return;
        }

        if (!_sceneDataMap.ContainsKey(type) || _sceneDataMap[type] == null)
        {
            Debug.LogWarning("[SceneLoadManager] No scene data provided.");
            return;
        }

        _nextSceneData = _sceneDataMap[type];
        _isLoading = true;
        StartCoroutine(LoadSceneAsync());
    }

    #endregion

    #region Private Coroutine LoadSceneAsync

    private IEnumerator LoadSceneAsync()
    {
        _loadingProgress = 0f;

        float startTime = Time.time;
        string sceneName = _nextSceneData.SceneName;
        ESceneLoadMode loadMode = _nextSceneData.SceneLoadMode;

        OnSceneLoadStart?.Invoke(sceneName);

        if (loadMode == ESceneLoadMode.Local)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            if (asyncLoad == null)
            {
                FailSceneLoad(($"[SceneLoadManager] Failed to load scene: {sceneName}"));
        
                yield break;
            }

            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                _loadingProgress = asyncLoad.progress;
                yield return null;
            }

            _loadingProgress = 1f;
            asyncLoad.allowSceneActivation = true;

            yield return asyncLoad;
            yield return Resources.UnloadUnusedAssets();
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(sceneName);
            while (PhotonNetwork.LevelLoadingProgress < 1f)
            {
                _loadingProgress = PhotonNetwork.LevelLoadingProgress;
                yield return null;
            }

            _loadingProgress = 1f;
            
        }
        FinishSceneLoad(true);
    }

    private void FinishSceneLoad(bool success)
    {
        if (success && _nextSceneData != null)
        {
            OnSceneLoadComplete?.Invoke(_nextSceneData.SceneName);
            Debug.Log($"[SceneLoadManager] SceneLoad Success");
        }

        _nextSceneData = null;
        _loadingProgress = 0f;
        _isLoading = false;
    }

    private void FailSceneLoad(string message)
    {
        Debug.LogError(message);
        FinishSceneLoad(false);
    }
    #endregion
}
