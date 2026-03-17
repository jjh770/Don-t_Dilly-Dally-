using UnityEngine;

[CreateAssetMenu(fileName = "APIKeyConfig", menuName = "Config/API Key Config")]
public class APIKeyConfig : ScriptableObject
{
    [Header("Gemini")]
    [SerializeField] private string _geminiApiKey;

    [Header("Google Cloud")]
    [SerializeField] private string _googleCloudApiKey;

    public string GeminiApiKey => _geminiApiKey;
    public string GoogleCloudApiKey => _googleCloudApiKey;
}
