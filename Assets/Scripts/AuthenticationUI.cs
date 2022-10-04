using System;
using System.Collections;
using System.Threading;
using Cdm.Authentication.Browser;
using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class AuthenticationUI : UIBehaviour
{
    public ScrollRect scrollView;
    public TMP_Text statusText;
    public Button authenticateButton;
    public Button refreshTokenButton;
    public Button userInfoButton;
    public TMP_InputField loginTimeoutInput;

    public GameObject iosBrowsers;
    public Toggle asWebAuthenticationSessionBrowserToggle;
    public Toggle wkWebViewAuthenticationSessionBrowserToggle;

    private CrossPlatformBrowser _crossPlatformBrowser;
    private AuthenticationSession _authenticationSession;
    private CancellationTokenSource _cancellationTokenSource;

    protected override void Awake()
    {
        base.Awake();

        statusText.text = "";
        Application.logMessageReceived += OnLogMessageReceived;

        iosBrowsers.SetActive(Application.platform == RuntimePlatform.IPhonePlayer);

        _crossPlatformBrowser = new CrossPlatformBrowser();
        _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
        _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
        _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
        _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
        _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer,
            new ASWebAuthenticationSessionBrowser());

        var configuration = new AuthorizationCodeFlow.Configuration()
        {
            clientId = AppAuth.ClientId,
            clientSecret = AppAuth.ClientSecret,
            redirectUri = AppAuth.RedirectUri,
            scope = AppAuth.Scope
        };
        
        var auth = new MockServerAuth(configuration, "http://localhost:8001");

        _authenticationSession = new AuthenticationSession(auth, _crossPlatformBrowser);

        authenticateButton.onClick.AddListener(AuthenticateAsync);
        refreshTokenButton.onClick.AddListener(RefreshTokenAsync);
        userInfoButton.onClick.AddListener(GetUserInfoAsync);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Application.logMessageReceived -= OnLogMessageReceived;

        _cancellationTokenSource?.Cancel();
        _authenticationSession?.Dispose();
    }

    private IEnumerator ScrollToEnd()
    {
        yield return new WaitForEndOfFrame();
        scrollView.normalizedPosition = Vector3.zero;
    }

    private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
    {
        switch (type)
        {
            case LogType.Warning:
                condition = $"<color=yellow>{condition}</color>";
                break;
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                condition = $"<color=red>{condition}</color>";
                break;
        }

        statusText.text += $"{condition}\n";

        StartCoroutine(ScrollToEnd());
    }

    private async void AuthenticateAsync()
    {
        if (_authenticationSession != null)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            _crossPlatformBrowser.platformBrowsers.Remove(RuntimePlatform.IPhonePlayer);
            if (asWebAuthenticationSessionBrowserToggle.isOn)
            {
                _crossPlatformBrowser.platformBrowsers.Add(
                    RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());
            }
            else if (wkWebViewAuthenticationSessionBrowserToggle.isOn)
            {
                _crossPlatformBrowser.platformBrowsers.Add(
                    RuntimePlatform.IPhonePlayer, new WKWebViewAuthenticationSessionBrowser());
            }

            if (double.TryParse(loginTimeoutInput.text, out var value))
            {
                _authenticationSession.loginTimeout = TimeSpan.FromSeconds(value);
            }

            try
            {
                var accessTokenResponse =
                    await _authenticationSession.AuthenticateAsync(_cancellationTokenSource.Token);

                Debug.Log(
                    $"Access token response:\n {JsonConvert.SerializeObject(accessTokenResponse, Formatting.Indented)}");
            }
            catch (AuthorizationCodeRequestException ex)
            {
                Debug.LogError($"{nameof(AuthorizationCodeRequestException)} " +
                               $"error: {ex.error.code}, description: {ex.error.description}, uri: {ex.error.uri}");
            }
            catch (AccessTokenRequestException ex)
            {
                Debug.LogError($"{nameof(AccessTokenRequestException)} " +
                               $"error: {ex.error.code}, description: {ex.error.description}, uri: {ex.error.uri}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    private async void RefreshTokenAsync()
    {
        if (_authenticationSession != null)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var accessTokenResponse =
                    await _authenticationSession.RefreshTokenAsync(_cancellationTokenSource.Token);

                Debug.Log(
                    $"Refresh token response:\n {JsonConvert.SerializeObject(accessTokenResponse, Formatting.Indented)}");
            }
            catch (AccessTokenRequestException ex)
            {
                Debug.LogError($"{nameof(AccessTokenRequestException)} " +
                               $"error: {ex.error.code}, description: {ex.error.description}, uri: {ex.error.uri}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
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
                Debug.Log($"User id: {userInfo.id}, name:{userInfo.name}, email: {userInfo.email}");
            }
        }
    }
}