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
    public event Action<ESceneType> OnSceneLoadStart;
    public event Action<ESceneType> OnSceneLoadComplete;
    #endregion

    private bool _isLoading = false;
    private float _loadingProgress = 0f;
    private SceneDataSO _nextSceneData;
    private ESceneType _currentSceneType;


    public bool IsLoading => _isLoading;
    public float LoadingProgress => _loadingProgress;
    public SceneDataSO NextSceneData => _nextSceneData;
    public ESceneType CurrentSceneType => _currentSceneType;
    private void Start()
    {
        foreach (SceneDataSO data in sceneDataSOs)
        {
            ESceneType type = data.SceneType;
            if (_sceneDataMap.ContainsKey(type))
            {
                Debug.LogWarning($"[SceneLoadManager] Duplicate scene type: {type}. Skipping.");
                continue;
            }
            _sceneDataMap[type] = data;
        }

        UpdateCurrentSceneType(SceneManager.GetActiveScene().name);
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

        SoundManager.Instance?.StopBGM();

        _nextSceneData = _sceneDataMap[type];
        _isLoading = true;
        StartCoroutine(LoadSceneAsync());
    }

    #endregion

    #region Private Coroutine LoadSceneAsync

    private IEnumerator LoadSceneAsync()
    {
        _loadingProgress = 0f;

        string sceneName = _nextSceneData.SceneName;

        ESceneLoadMode loadMode = _nextSceneData.SceneLoadMode;

        OnSceneLoadStart?.Invoke(_nextSceneData.SceneType);

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
        }

        if (loadMode == ESceneLoadMode.PhotonSynced)
        {
            while (!IsTargetSceneLoaded(sceneName))
            {
                _loadingProgress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress);
                yield return null;
            }

            _loadingProgress = 1f;
            yield return null;
            yield return Resources.UnloadUnusedAssets();
        }

        FinishSceneLoad(true);
    }

    private void FinishSceneLoad(bool success)
    {
        if (success && _nextSceneData != null)
        {
            _currentSceneType = _nextSceneData.SceneType;
            OnSceneLoadComplete?.Invoke(_nextSceneData.SceneType);
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

    private void UpdateCurrentSceneType(string sceneName)
    {
        foreach (SceneDataSO data in _sceneDataMap.Values)
        {
            if (data != null && data.SceneName == sceneName)
            {
                _currentSceneType = data.SceneType;
                return;
            }
        }
    }

    private static bool IsTargetSceneLoaded(string sceneName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() && activeScene.name == sceneName;
    }
    #endregion
}
