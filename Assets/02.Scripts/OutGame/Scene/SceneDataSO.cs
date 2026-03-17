using UnityEngine;


/// 씬 정보를 저장하는 ScriptableObject
[CreateAssetMenu(fileName = "New Scene Data", menuName = "Game/Scene Data", order = 1)]
public class SceneDataSO : ScriptableObject
{
    #region Inspector Fields
    [Header("Scene Info")]
    [Tooltip("씬 이름 (Build Settings의 씬 이름과 일치해야 함)")]
    [SerializeField] private string _sceneName;

    [Tooltip("씬 설명")]
    [TextArea(3, 5)]
    [SerializeField] private string _description;

    [Header("Scene Properties")]
    [Tooltip("씬 타입")]
    [SerializeField] private ESceneType _sceneType = ESceneType.Gameplay;

    [Tooltip("씬 로드 모드")]
    [SerializeField] private ESceneLoadMode _sceneLoadMode = ESceneLoadMode.Local;
    #endregion

    #region Public Properties

    public string SceneName => _sceneName;
    public string Description => _description;

    public ESceneType SceneType => _sceneType;

    public ESceneLoadMode SceneLoadMode => _sceneLoadMode;

    #endregion

    #region Validation
    private void OnValidate()
    {
        // 씬 이름이 비어있으면 경고
        if (string.IsNullOrEmpty(_sceneName))
        {
            Debug.LogWarning($"[SceneData] {name}: Scene name is empty!");
        }
    }
    #endregion
}