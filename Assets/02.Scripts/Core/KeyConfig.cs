using UnityEngine;

[CreateAssetMenu(fileName = "APIKeyConfig", menuName = "Config/API Key Config")]
public class KeyConfig : ScriptableObject
{
    [Header("Gemini")]
    [SerializeField] private string _geminiApiKey;

    [Header("Google Cloud")]
    [SerializeField] private string _googleCloudApiKey;

    [Header("Google Cloud")]
    [SerializeField] private string _clientId;
    [SerializeField] private string _clientSecret;

    public string GeminiApiKey => _geminiApiKey;
    public string GoogleCloudApiKey => _googleCloudApiKey;

    public string CLIENT_ID => _clientId;
    public string CLIENT_SECRET => _clientSecret;
}
