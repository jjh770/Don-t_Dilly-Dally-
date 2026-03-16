using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMService : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private APIKeyConfig _apiKeyConfig;
    [SerializeField] private string _apiUrl = "https://api.openai.com/v1/chat/completions";
    [SerializeField] private string _model = "gpt-4o-mini";

    [Header("Request Settings")]
    [SerializeField] private int _maxTokens = 100;
    [SerializeField] private float _temperature = 0.7f;
    [SerializeField] private float _timeout = 10f;

    public async Awaitable<string> SendRequest(string systemPrompt, string userPrompt)
    {
        if (_apiKeyConfig == null || string.IsNullOrEmpty(_apiKeyConfig.OpenAIApiKey))
        {
            Debug.LogError("[LLMService] API Key Config is not set");
            return null;
        }

        string requestBody = BuildRequestBody(systemPrompt, userPrompt);

        using UnityWebRequest request = new UnityWebRequest(_apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {_apiKeyConfig.OpenAIApiKey}");
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
            ""model"": ""{_model}"",
            ""messages"": [
                {{""role"": ""system"", ""content"": ""{EscapeJson(systemPrompt)}""}},
                {{""role"": ""user"", ""content"": ""{EscapeJson(userPrompt)}""}}
            ],
            ""max_tokens"": {_maxTokens},
            ""temperature"": {_temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)}
        }}";
    }

    private string ParseResponse(string json)
    {
        try
        {
            LLMResponse response = JsonUtility.FromJson<LLMResponse>(json);
            if (response?.choices != null && response.choices.Length > 0)
            {
                return response.choices[0].message.content;
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
    private class LLMResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public Message message;
    }

    [Serializable]
    private class Message
    {
        public string content;
    }
}
