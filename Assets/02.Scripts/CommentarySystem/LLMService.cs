using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMService : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private APIKeyConfig _apiKeyConfig;
    [SerializeField] private string _model = "gemini-3.1-flash-lite-preview";

    [Header("Request Settings")]
    [SerializeField] private int _maxTokens = 100;
    [SerializeField] private float _temperature = 0.7f;
    [SerializeField] private float _timeout = 10f;

    private const string ApiUrlFormat = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    public async Awaitable<string> SendRequest(string systemPrompt, string userPrompt)
    {
        if (_apiKeyConfig == null || string.IsNullOrEmpty(_apiKeyConfig.GeminiApiKey))
        {
            Debug.LogError("[LLMService] API Key Config is not set");
            return null;
        }

        string apiUrl = string.Format(ApiUrlFormat, _model, _apiKeyConfig.GeminiApiKey);
        string requestBody = BuildRequestBody(systemPrompt, userPrompt);

        using UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = (int)_timeout;

        try
        {
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LLMService] Request failed: {request.error}");
                return null;
            }

            string response = request.downloadHandler.text;
            return ParseResponse(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLMService] Exception: {e.Message}");
            return null;
        }
    }

    private string BuildRequestBody(string systemPrompt, string userPrompt)
    {
        return $@"{{
            ""contents"": [
                {{
                    ""role"": ""user"",
                    ""parts"": [{{""text"": ""{EscapeJson(userPrompt)}""}}]
                }}
            ],
            ""systemInstruction"": {{
                ""parts"": [{{""text"": ""{EscapeJson(systemPrompt)}""}}]
            }},
            ""generationConfig"": {{
                ""temperature"": {_temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""maxOutputTokens"": {_maxTokens}
            }}
        }}";
    }

    private string ParseResponse(string json)
    {
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(json);
            if (response?.candidates != null && response.candidates.Length > 0)
            {
                var parts = response.candidates[0].content.parts;
                if (parts != null && parts.Length > 0)
                {
                    return parts[0].text;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLMService] Failed to parse response: {e.Message}");
        }

        return null;
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

    [Serializable]
    private class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [Serializable]
    private class Candidate
    {
        public Content content;
    }

    [Serializable]
    private class Content
    {
        public Part[] parts;
    }

    [Serializable]
    private class Part
    {
        public string text;
    }
}
