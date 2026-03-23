using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class GoogleAuthManager : MonoBehaviour
{
    [SerializeField] private KeyConfig _keyConfig;

    private const string REDIRECT_URI = "http://localhost:5000/callback";

    private const string AUTH_URL = "https://accounts.google.com/o/oauth2/auth";
    private const string TOKEN_URL = "https://oauth2.googleapis.com/token";
    private const string USER_INFO_URL = "https://www.googleapis.com/oauth2/v2/userinfo";

    private HttpListener _httpListener;
    private string _authCode;

    public void Start()
    {
        // 예시: 버튼 클릭 시 로그인 시작
        StartGoogleLogin();
    }

    public async void StartGoogleLogin()
    {
        // 1. 브라우저로 구글 로그인 페이지 열기
        string authUri = $"{AUTH_URL}" +
            $"?client_id={_keyConfig.CLIENT_ID}" +
            $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
            $"&response_type=code" +
            $"&scope=openid%20email%20profile";

        Application.OpenURL(authUri);

        // 2. 로컬 서버로 콜백 수신
        _authCode = await WaitForCallbackCode();

        if (string.IsNullOrEmpty(_authCode)) return;

        // 3. Access Token 교환
        string accessToken = await ExchangeCodeForToken(_authCode);

        if (string.IsNullOrEmpty(accessToken)) return;

        // 4. 유저 정보 가져오기
        await GetUserInfo(accessToken);
    }

    // 로컬 HTTP 서버로 구글 콜백 수신
    private async Task<string> WaitForCallbackCode()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://localhost:5000/");
        _httpListener.Start();

        Debug.Log("구글 로그인 대기중...");

        var context = await _httpListener.GetContextAsync();
        var request = context.Request;
        string error = context.Request.QueryString["error"];

        // 브라우저에 완료 메시지 표시
        context.Response.ContentType = "text/html; charset=utf-8";

        string responseHtml = string.IsNullOrEmpty(error)
            ? "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif;text-align:center;padding-top:100px'><h2>✅ 로그인 완료! 게임으로 돌아가세요.</h2></body></html>"
            : "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif;text-align:center;padding-top:100px'><h2>❌ 로그인 실패. 다시 시도해주세요.</h2></body></html>";

        var buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        context.Response.Close();

        _httpListener.Stop();

        // URL에서 code 파라미터 추출
        string code = request.QueryString["code"];
        return code;
    }

    // Authorization Code → Access Token 교환
    private async Task<string> ExchangeCodeForToken(string code)
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

        var tokenData = JsonUtility.FromJson<TokenResponse>(json);
        return tokenData?.access_token;
    }

    // 유저 정보 조회
    private async Task<GoogleUserInfo> GetUserInfo(string accessToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var response = await client.GetAsync(USER_INFO_URL);
        var json = await response.Content.ReadAsStringAsync();

        var userInfo = JsonUtility.FromJson<GoogleUserInfo>(json);

        Debug.Log($"로그인 성공! 이름: {userInfo.name}, 이메일: {userInfo.email}");
        OnLoginSuccess(userInfo);

        return userInfo;
    }

    private void OnLoginSuccess(GoogleUserInfo userInfo)
    {
        // 여기서 로그인 후 처리 (씬 전환, UI 업데이트 등)
    }
}

[Serializable]
public class TokenResponse
{
    public string access_token;
    public string refresh_token;
    public int expires_in;
}

[Serializable]
public class GoogleUserInfo
{
    public string id;
    public string email;
    public string name;
    public string picture; // 프로필 이미지 URL
}
