using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMService : MonoBehaviour
{
    [Header("API 설정")]
    [SerializeField] private KeyConfig _apiKeyConfig;
    [SerializeField] private string _model = "gemini-3.1-flash-lite-preview";

    [Header("요청 설정")]
    [SerializeField] private int _maxTokens = 100;      // AI가 최대 몇 토큰까지 답할지
    [SerializeField] private float _temperature = 0.7f; // 답변의 랜덤성 정도 (높을수록 다양)
    [SerializeField] private float _timeout = 10f;      // 요청 제한 시간 (초)

    private const string ApiUrlFormat = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    // systemPrompt와 userPrompt를 받아서
    // Gemini에 요청 보내고
    // 최종적으로 문자열 하나를 돌려줌
    public async Awaitable<string> SendRequest(string systemPrompt, string userPrompt)
    {
        if (_apiKeyConfig == null || string.IsNullOrEmpty(_apiKeyConfig.GeminiApiKey))
        {
            Debug.LogError("[LLMService] API Key가 없습니다.");
            return null;
        }

        string apiUrl = string.Format(ApiUrlFormat, _model, _apiKeyConfig.GeminiApiKey);
        string requestBody = BuildRequestBody(systemPrompt, userPrompt); // JSON 형식으로 변환

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
                Debug.LogError($"[LLMService] 요청 실패: {request.error}");
                return null;
            }

            string response = request.downloadHandler.text; // 응답 텍스트 꺼내고 (JSON)
            return ParseResponse(response);                 // 진짜 답변만 추출하기
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLMService] 예외: {e.Message}");
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
            Debug.LogError($"[LLMService] 응답 파싱 실패: {e.Message}");
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
