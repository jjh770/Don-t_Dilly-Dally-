using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;

public class GoogleAuthManager : MonoBehaviour
{
    [SerializeField] private KeyConfig _keyConfig;
    [SerializeField] private float _timeoutInMinutes = 1.0f;

    private const string REDIRECT_URI = "http://localhost:5000/callback";
    private const string AUTH_URL = "https://accounts.google.com/o/oauth2/auth";
    private const string TOKEN_URL = "https://oauth2.googleapis.com/token";
    private const string USER_INFO_URL = "https://www.googleapis.com/oauth2/v2/userinfo";

    private HttpListener _httpListener;
    private string _authCode;

    // ✅ 추가: 취소 토큰 소스
    private CancellationTokenSource _cts;

    public async UniTask StartGoogleLogin()
    {
        // ✅ 기존 취소 토큰 정리 후 새로 생성
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        string authUri = $"{AUTH_URL}" +
            $"?client_id={_keyConfig.CLIENT_ID}" +
            $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
            $"&response_type=code" +
            $"&scope=openid%20email%20profile";

        Application.OpenURL(authUri);

        _authCode = await WaitForCallbackCode(_cts.Token);

        if (string.IsNullOrEmpty(_authCode)) return;

        // 1. 토큰 교환 (id_token과 access_token을 모두 가져옴)
        var tokenResponse = await ExchangeCodeForTokenResponse(_authCode);

        if (tokenResponse == null)
        {
            Debug.LogError("Google 토큰 교환 실패: 응답이 비어 있습니다.");
            return;
        }

        if (!string.IsNullOrEmpty(tokenResponse.error))
        {
            Debug.LogError($"Google 토큰 교환 실패: {tokenResponse.error} / {tokenResponse.error_description}");
            return;
        }

        if (string.IsNullOrEmpty(tokenResponse.id_token))
        {
            Debug.LogError("Google 토큰 교환 실패: id_token이 비어 있습니다.");
            return;
        }

        // 2. Firebase 로그인 호출
        await SignInWithFirebase(tokenResponse.id_token);
    }

    // ✅ 추가: 취소 버튼에 연결할 메서드
    public void CancelLogin()
    {
        if (_cts == null || _cts.IsCancellationRequested) return;

        Debug.Log("로그인 취소 요청");
        _cts.Cancel();
    }

    private async Task<string> WaitForCallbackCode(CancellationToken cancellationToken)
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://localhost:5000/");
        _httpListener.Start();

        Debug.Log("구글 로그인 대기중...");

        // ✅ 취소 시 HttpListener를 Stop하여 GetContextAsync()를 중단
        cancellationToken.Register(() =>
        {
            _httpListener?.Stop();
        });

        try
        {
            var contextTask = _httpListener.GetContextAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(_timeoutInMinutes), cancellationToken);

            // ✅ 취소되면 timeoutTask가 OperationCanceledException을 throw
            var completed = await Task.WhenAny(contextTask, timeoutTask);

            if (completed == timeoutTask)
            {
                // 타임아웃이 취소로 인한 것인지 확인
                cancellationToken.ThrowIfCancellationRequested();

                Debug.Log("로그인 시간이 초과되었습니다.");
                _cts.Cancel(); // httpListener.Stop()이 Register에 등록되어 있으므로 정리까지 처리됨
                return null;
            }

            var context = await contextTask;
            var request = context.Request;
            string error = request.QueryString["error"];

            context.Response.ContentType = "text/html; charset=utf-8";
            string responseHtml = string.IsNullOrEmpty(error)
                ? "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif;text-align:center;padding-top:100px'><h2>✅ 로그인 완료! 게임으로 돌아가세요.</h2></body></html>"
                : "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif;text-align:center;padding-top:100px'><h2>❌ 로그인 실패. 다시 시도해주세요.</h2></body></html>";

            var buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();

            return request.QueryString["code"];
        }
        catch (OperationCanceledException)
        {
            // ✅ 취소 처리
            Debug.Log("로그인이 취소되었습니다.");
            return null;
        }
        catch (HttpListenerException)
        {
            // ✅ _httpListener.Stop() 호출 시 GetContextAsync가 여기로 빠짐
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.Log("로그인이 취소되었습니다.");
                return null;
            }
            throw;
        }
        finally
        {
            // ✅ 항상 리스너 정리
            if (_httpListener.IsListening)
                _httpListener.Stop();
        }
    }

    private async Task<TokenResponse> ExchangeCodeForTokenResponse(string code)
    {
        using var client = new HttpClient();

        var parameters = new System.Collections.Generic.Dictionary<string, string>
        {
            { "code", code },
            { "client_id", _keyConfig.CLIENT_ID },
            { "client_secret", _keyConfig.CLIENT_SECRET },
            { "redirect_uri", REDIRECT_URI },
            { "grant_type", "authorization_code" }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await client.PostAsync(TOKEN_URL, content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"Google 토큰 교환 HTTP 실패: {(int)response.StatusCode} / {json}");
        }

        return JsonUtility.FromJson<TokenResponse>(json);
    }

    private async Task SignInWithFirebase(string googleIdToken)
    {
        try
        {
            Credential credential = GoogleAuthProvider.GetCredential(googleIdToken, null);
            var auth = FirebaseAuth.DefaultInstance;

            // ContinueWith 대신 await 사용
            FirebaseUser newUser = await auth.SignInWithCredentialAsync(credential);

            Debug.Log($"Firebase 로그인 완료: {newUser.DisplayName} (Email: {newUser.Email}, UID: {newUser.UserId})");

            PlayerDataManager.Instance.SetPlayerID(newUser.UserId);
            SceneLoadManager.Instance.BeginSceneLoad(ESceneType.Lobby);
        }
        catch (Exception e)
        {
            Debug.LogError($"Firebase 로그인 실패: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        // ✅ 씬 전환 등으로 오브젝트 파괴 시 정리
        _cts?.Cancel();
        _cts?.Dispose();
    }
}

[Serializable]
public class TokenResponse
{
    public string access_token;
    public string id_token;
    public string refresh_token;
    public int expires_in;
    public string error;
    public string error_description;
}
