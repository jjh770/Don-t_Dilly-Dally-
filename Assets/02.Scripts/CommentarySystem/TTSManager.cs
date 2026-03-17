using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TTSManager : MonoBehaviour
{
    private const string ApiUrl = "https://texttospeech.googleapis.com/v1/text:synthesize";
    private const int SampleRate = 24000;

    [Header("API Settings")]
    [SerializeField] private APIKeyConfig _apiKeyConfig;

    [Header("Voice Settings")]
    [SerializeField] private string _languageCode = "ko-KR";
    [SerializeField] private string _voiceName = "ko-KR-Wavenet-A";
    [SerializeField] private float _speakingRate = 1.0f;
    [SerializeField] private float _pitch = 0f;

    [Header("Request Settings")]
    [SerializeField] private float _timeout = 15f;

    public async Awaitable<AudioClip> GenerateSpeech(string text)
    {
        if (_apiKeyConfig == null || string.IsNullOrEmpty(_apiKeyConfig.GoogleCloudApiKey))
        {
            Debug.LogError("[TTSManager] Google Cloud API 키가 없습니다.");
            return null;
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[TTSManager] 텍스트가 비어있습니다.");
            return null;
        }

        string url = $"{ApiUrl}?key={_apiKeyConfig.GoogleCloudApiKey}";
        string requestBody = BuildRequestBody(text);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
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
                Debug.LogError($"[TTSManager] 응답 실패: {request.error}");
                return null;
            }

            string response = request.downloadHandler.text;
            return ParseResponseToAudioClip(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TTSManager] 예외: {e.Message}");
            return null;
        }
    }

    private string BuildRequestBody(string text)
    {
        return $@"{{
            ""input"": {{
                ""text"": ""{EscapeJson(text)}""
            }},
            ""voice"": {{
                ""languageCode"": ""{_languageCode}"",
                ""name"": ""{_voiceName}""
            }},
            ""audioConfig"": {{
                ""audioEncoding"": ""LINEAR16"",
                ""sampleRateHertz"": {SampleRate},
                ""speakingRate"": {_speakingRate.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""pitch"": {_pitch.ToString(System.Globalization.CultureInfo.InvariantCulture)}
            }}
        }}";
    }

    private AudioClip ParseResponseToAudioClip(string json)
    {
        try
        {
            TTSResponse response = JsonUtility.FromJson<TTSResponse>(json);
            if (string.IsNullOrEmpty(response?.audioContent))
            {
                Debug.LogError("[TTSManager] 음성 데이터가 없음");
                return null;
            }

            byte[] audioBytes = Convert.FromBase64String(response.audioContent);
            float[] samples = ConvertBytesToFloats(audioBytes);

            AudioClip clip = AudioClip.Create("TTS", samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TTSManager] 응답 파싱 실패: {e.Message}");
            return null;
        }
    }

    private float[] ConvertBytesToFloats(byte[] bytes)
    {
        int sampleCount = bytes.Length / 2;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = BitConverter.ToInt16(bytes, i * 2);
            samples[i] = sample / 32768f;
        }

        return samples;
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
    private class TTSResponse
    {
        public string audioContent;
    }
}
