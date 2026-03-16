using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TTSManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private APIKeyConfig _apiKeyConfig;
    [SerializeField] private string _apiUrl = "https://api.openai.com/v1/audio/speech";

    [Header("Voice Settings")]
    [SerializeField] private string _model = "tts-1";
    [SerializeField] private string _voice = "nova";
    [SerializeField] private float _speed = 1.0f;

    [Header("Request Settings")]
    [SerializeField] private float _timeout = 15f;

    public async Awaitable<AudioClip> GenerateSpeech(string text)
    {
        if (_apiKeyConfig == null || string.IsNullOrEmpty(_apiKeyConfig.OpenAIApiKey))
        {
            Debug.LogError("[TTSManager] API Key Config is not set");
            return null;
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[TTSManager] Text is empty");
            return null;
        }

        string requestBody = BuildRequestBody(text);

        using UnityWebRequest request = new UnityWebRequest(_apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerAudioClip(_apiUrl, AudioType.MPEG);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {_apiKeyConfig.OpenAIApiKey}");
        request.timeout = (int)_timeout;

        try
        {
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[TTSManager] Request failed: {request.error}");
                return null;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TTSManager] Exception: {e.Message}");
            return null;
        }
    }

    private string BuildRequestBody(string text)
    {
        return $@"{{
            ""model"": ""{_model}"",
            ""input"": ""{EscapeJson(text)}"",
            ""voice"": ""{_voice}"",
            ""speed"": {_speed.ToString(System.Globalization.CultureInfo.InvariantCulture)},
            ""response_format"": ""mp3""
        }}";
    }

    private string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
