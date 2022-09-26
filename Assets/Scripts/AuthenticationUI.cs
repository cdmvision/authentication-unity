using System.Threading;
using Cdm.Authentication.Browser;
using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticationUI : MonoBehaviour
{
    public Button authenticateButton;
    public Button refreshTokenButton;
    public Button userInfoButton;

    private AuthenticationSession _authenticationSession;
    private CancellationTokenSource _cancellationTokenSource;

    private void Awake()
    {
        var configurationText = Resources.Load<TextAsset>("Configuration");
        if (configurationText == null)
        {
            Debug.LogError("Auth client configuration could not found at Resources/Configuration.json.");
            return;
        }

        var browser = new CrossPlatformBrowser();
        browser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
        browser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
        browser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
        browser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
        browser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());

        var configuration =
            JsonConvert.DeserializeObject<AuthorizationCodeFlow.Configuration>(configurationText.text);

        var auth = new GoogleAuth(configuration);

        _authenticationSession = new AuthenticationSession(auth, browser);

        authenticateButton.onClick.AddListener(AuthenticateAsync);
        refreshTokenButton.onClick.AddListener(RefreshTokenAsync);
        userInfoButton.onClick.AddListener(GetUserInfoAsync);
    }

    private async void AuthenticateAsync()
    {
        if (_authenticationSession != null)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            await _authenticationSession.AuthenticateAsync(_cancellationTokenSource.Token);
        }
    }

    private async void RefreshTokenAsync()
    {
        if (_authenticationSession != null)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            await _authenticationSession.RefreshTokenAsync(_cancellationTokenSource.Token);
        }
    }

    private async void GetUserInfoAsync()
    {
        if (_authenticationSession != null)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            if (_authenticationSession.SupportsUserInfo())
            {
                var userInfo = await _authenticationSession.GetUserInfoAsync(_cancellationTokenSource.Token);
            }
        }
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Cancel();
        _authenticationSession?.Dispose();
    }
}