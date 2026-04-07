using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DontDillyDally.Data
{
    // Gemini API를 호출하여 질병 JSON을 생성하는 서비스입니다.
    // LLMService와 동일한 패턴이지만 질병 생성에 맞는 별도 설정을 사용합니다.
    public class DiseaseGenerationService : MonoBehaviour
    {
        [Header("API 설정")]
        [SerializeField] private KeyConfig _apiKeyConfig;
        [SerializeField] private string _model = "gemini-3.1-flash-lite-preview";

        [Header("요청 설정")]
        [SerializeField] private int _maxTokens = 4096;
        [SerializeField] private float _temperature = 0.85f;
        [SerializeField] private float _timeout = 30f;

        [Header("프롬프트")]
        [SerializeField] private TextAsset _systemPromptFile;

        private const string ApiUrlFormat =
            "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

        // 사용자 프롬프트를 받아 Gemini에 질병 생성을 요청하고 JSON 문자열을 반환합니다.
        public async Awaitable<string> GenerateDiseaseJson(string userPrompt)
        {
            if (_apiKeyConfig == null || string.IsNullOrEmpty(_apiKeyConfig.GeminiApiKey))
            {
                Debug.LogWarning("[DiseaseGenerationService] API Key가 설정되지 않았습니다. APIKeyConfig SO를 연결하세요.");
                return null;
            }

            if (_systemPromptFile == null)
            {
                Debug.LogWarning("[DiseaseGenerationService] 시스템 프롬프트 파일이 연결되지 않았습니다.");
                return null;
            }

            string apiUrl = string.Format(ApiUrlFormat, _model, _apiKeyConfig.GeminiApiKey);
            string systemPrompt = _systemPromptFile.text;
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
                    Debug.LogWarning($"[DiseaseGenerationService] 요청 실패: {request.error}");
                    return null;
                }

                string response = request.downloadHandler.text;
                string generatedText = ParseResponse(response);

                if (string.IsNullOrEmpty(generatedText))
                {
                    Debug.LogWarning("[DiseaseGenerationService] 응답에서 텍스트를 추출할 수 없습니다.");
                    return null;
                }

                return ExtractJson(generatedText);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DiseaseGenerationService] 예외: {e.Message}");
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
                        return parts[0].text;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DiseaseGenerationService] 응답 파싱 실패: {e.Message}");
            }

            return null;
        }

        // Gemini가 ```json ... ``` 마크다운으로 감싸는 경우를 대비하여
        // 첫 번째 '{' 부터 마지막 '}' 까지만 추출합니다.
        private string ExtractJson(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');

            if (start < 0 || end < 0 || end <= start)
            {
                Debug.LogWarning("[DiseaseGenerationService] 응답에서 JSON 객체를 찾을 수 없습니다.");
                return null;
            }

            return raw.Substring(start, end - start + 1);
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 체인 Replace 대신 StringBuilder로 단일 순회하여 중간 문자열 할당을 방지합니다.
            var stringBuilder = new StringBuilder(text.Length + 32);
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                switch (character)
                {
                    case '\\': stringBuilder.Append("\\\\"); break;
                    case '"': stringBuilder.Append("\\\""); break;
                    case '\n': stringBuilder.Append("\\n"); break;
                    case '\r': stringBuilder.Append("\\r"); break;
                    case '\t': stringBuilder.Append("\\t"); break;
                    default: stringBuilder.Append(character); break;
                }
            }

            return stringBuilder.ToString();
        }

        // [명명 규칙 예외] Gemini API JSON 응답 역직렬화용 DTO입니다.
        // JsonUtility가 필드명과 JSON 키를 1:1 매칭하므로 camelCase를 사용합니다.
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
}
