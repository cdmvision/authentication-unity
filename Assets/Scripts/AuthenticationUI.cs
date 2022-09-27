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

    private AuthenticationSession _authenticationSession;
    private CancellationTokenSource _cancellationTokenSource;

    protected override void Awake()
    {
        base.Awake();

        statusText.text = "";
        Application.logMessageReceived += OnLogMessageReceived;

        if (!AuthConfigurationLoader.TryLoad(out var configuration))
            return;

        var browser = new CrossPlatformBrowser();
        browser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
        browser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
        browser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
        browser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
        browser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());

        var auth = new GoogleAuth(configuration);

        _authenticationSession = new AuthenticationSession(auth, browser);

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

            if (double.TryParse(loginTimeoutInput.text, out var value))
            {
                _authenticationSession.loginTimeout = TimeSpan.FromSeconds(value);
            }
            
            var accessTokenResponse = await _authenticationSession.AuthenticateAsync(_cancellationTokenSource.Token);

            Debug.Log(
                $"Access token response:\n {JsonConvert.SerializeObject(accessTokenResponse, Formatting.Indented)}");
        }
    }

    private async void RefreshTokenAsync()
    {
        if (_authenticationSession != null)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            var accessTokenResponse = await _authenticationSession.RefreshTokenAsync(_cancellationTokenSource.Token);

            Debug.Log(
                $"Refresh token response:\n {JsonConvert.SerializeObject(accessTokenResponse, Formatting.Indented)}");
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