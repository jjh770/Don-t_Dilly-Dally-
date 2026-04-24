using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TTSManager : MonoBehaviour
{
    private const string ApiUrl = "https://api.elevenlabs.io/v1/text-to-speech";

    [Header("API 세팅")]
    [SerializeField] private KeyConfig _apiKeyConfig;

    [Header("음성 세팅")]
    [SerializeField] private string _modelId = "eleven_multilingual_v2";
    [SerializeField] private float _stability = 0.5f;
    [SerializeField] private float _similarityBoost = 0.75f;
    [SerializeField, Range(0.25f, 4f)] private float _speed = 1.15f;

    [Header("요청 세팅")]
    [SerializeField] private float _timeout = 15f;

    public async Awaitable<AudioClip> GenerateSpeech(string text)
    {
        if (_apiKeyConfig == null || string.IsNullOrEmpty(_apiKeyConfig.ElevenLabsApiKey))
        {
            Debug.LogError("[TTSManager] ElevenLabs API 키가 없습니다.");
            return null;
        }

        if (string.IsNullOrEmpty(_apiKeyConfig.ElevenLabsVoiceId))
        {
            Debug.LogError("[TTSManager] ElevenLabs Voice ID가 없습니다.");
            return null;
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[TTSManager] 텍스트가 비어있습니다.");
            return null;
        }

        string url = $"{ApiUrl}/{_apiKeyConfig.ElevenLabsVoiceId}";
        string requestBody = BuildRequestBody(text);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("xi-api-key", _apiKeyConfig.ElevenLabsApiKey.Trim());
        request.SetRequestHeader("Accept", "audio/mpeg");
        request.timeout = (int)_timeout;

        try
        {
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[TTSManager] 응답 실패: {request.error}");
                Debug.LogWarning($"[TTSManager] 응답 코드: {request.responseCode}");
                Debug.LogWarning($"[TTSManager] 응답 본문: {request.downloadHandler.text}");
                return null;
            }

            byte[] audioData = request.downloadHandler.data;
            return await ConvertMp3ToAudioClip(audioData);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TTSManager] 예외: {e.Message}");
            return null;
        }
    }

    private string BuildRequestBody(string text)
    {
        return $@"{{
            ""text"": ""{EscapeJson(text)}"",
            ""model_id"": ""{_modelId}"",
            ""voice_settings"": {{
                ""stability"": {_stability.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""similarity_boost"": {_similarityBoost.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""speed"": {_speed.ToString(System.Globalization.CultureInfo.InvariantCulture)}
            }}
        }}";
    }

    private async Awaitable<AudioClip> ConvertMp3ToAudioClip(byte[] mp3Data)
    {
        string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"tts_{Guid.NewGuid()}.mp3");

        try
        {
            await System.IO.File.WriteAllBytesAsync(tempPath, mp3Data);

            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[TTSManager] MP3 로드 실패: {request.error}");
                return null;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TTSManager] MP3 변환 실패: {e.Message}");
            return null;
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
        }
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
