# Authentication Package for Unity
- OAuth2
  - Authorization code flow (https://www.rfc-editor.org/rfc/rfc6749#section-4.1)
  - Authorization code flow with PKCE (Proof Key for Code Exchange) (https://www.rfc-editor.org/rfc/rfc7636)
- Built-in clients
  - GitHub
  - Google
  - Facebook
- Built-in browser support
  - Deep link browser (iOS, Android, Universal Windows Platform and macOS) (https://docs.unity3d.com/Manual/deep-linking.html)
  - Standalone as local server (Windows, macOS and Linux)
  - ASWebAuthenticationSession (iOS) (https://developer.apple.com/documentation/authenticationservices/aswebauthenticationsession)
  - WKWebView (iOS) (https://developer.apple.com/documentation/webkit/wkwebview)

## Install via Unity Package Manager:
* Add `"com.cdm.authentication": "https://github.com/cdmvision/authentication-unity.git#1.2.0"` to your project's package manifest file in dependencies section.
* Or, `Package Manager > Add package from git URL...` and paste this URL: `https://github.com/cdmvision/authentication-unity.git#1.2.0`

## Example usage

You should create your client auth configuration:
```csharp
// Also you can use your own client configuration.
var auth = new GoogleAuth()
{
  clientId = "...",
  redirectUrl = "...",
  scope = "openid email profile"
};
```

Authentication session is created with auth configuration and a browser:
```csharp
using var authenticationSession = new AuthenticationSession(auth, new StandaloneBrowser());
```

Also you can use different browsers for each platform by using cross platform browser:
```csharp
var crossPlatformBrowser = new CrossPlatformBrowser();
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());

using var authenticationSession = new AuthenticationSession(auth, crossPlatformBrowser);

// Opens a browser to log user in
AccessTokenResponse accessTokenResponse = await authenticationSession.AuthenticateAsync();

// Authentication header can be used to make authorized http calls.
AuthenticationHeaderValue authenticationHeader = accessTokenResponse.GetAuthenticationHeader();

// Gets the current acccess token, or refreshes if it is expired.
accessTokenResponse = await authenticationSession.GetOrRefreshTokenAsync();

// Gets new access token by using the refresh token.
AccessTokenResponse newAccessTokenResponse = await authenticationSession.RefreshTokenAsync();

// Or you can get new access token with specified refresh token (i.e. stored on the local disk to prevent multiple sign-in for each app launch)
newAccessTokenResponse = await authenticationSession.RefreshTokenAsync("my_refresh_token");
```

## Browsers

`StandaloneBrowser` (desktops) opens the sign-in page with `Application.OpenURL` and listens on the loopback address for the redirect (RFC 8252, section 7.3). The redirect URL has to be `http` on a loopback host, for example `http://127.0.0.1:{port}/callback`. Only a GET of its path ends the wait. `GetAvailableLoopbackPort()` gives a free port, `openUrl` replaces the action that opens the page, and `closePageResponse` is the page the browser shows at the end.

`ASWebAuthenticationSessionBrowser` (iOS) shows Apple's sign-in sheet and returns the redirect to the app's own scheme, for example `com.example.app:/callback`. Set `prefersEphemeralWebBrowserSession` to keep the sheet from sharing Safari's cookies.

## Running the flow yourself

When the token request is not the standard one, run the browser part with `Pkce` and `AuthorizationRedirect`, and send the code and verifier to your own endpoint:
```csharp
var codeVerifier = Pkce.CreateCodeVerifier();
var state = Pkce.CreateState();
var loginUrl = $"https://server.example/authorize?redirect_uri={Uri.EscapeDataString(redirectUrl)}" +
               $"&code_challenge={Pkce.CreateCodeChallenge(codeVerifier)}" +
               $"&code_challenge_method={Pkce.ChallengeMethod}&state={state}";

var result = await browser.StartAsync(loginUrl, redirectUrl, cancellationToken);
if (result.status != BrowserStatus.Success)
    return;

var redirect = AuthorizationRedirect.Parse(result.redirectUrl);
if (!redirect.HasState(state))
    throw new SecurityException("The redirect does not belong to this sign-in.");
if (redirect.error != null)
    throw new Exception($"Sign-in refused: {redirect.error}");

// Exchange redirect.code and codeVerifier for tokens at your server.
```
