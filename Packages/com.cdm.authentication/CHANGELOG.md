# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-11-13
### Added
- `Pkce`: code verifier, S256 challenge and state, for clients that run the authorization code flow themselves. `AuthorizationCodeFlowWithPkce` uses it; the values it sends are made as before.
- `AuthorizationRedirect`: reads a redirect URL (http, or an app's own scheme such as `com.example.app:/callback`) into code, state and error, the error as any string. `AuthorizationCodeFlow.ExchangeCodeForAccessTokenAsync` reads redirects with it and behaves as before.
- `StandaloneBrowser.openUrl`: the action that opens the sign-in page, `Application.OpenURL` by default.
- `StandaloneBrowser.GetAvailableLoopbackPort()`.

### Changed
- `StandaloneBrowser` ends the wait only on a GET of the redirect URL's path. 1.2.0 ended it on the first request of any method under the prefix, so a favicon request could end a sign-in; now any other request is answered 404 and the wait goes on.
- `StandaloneBrowser` requires an http redirect URL on a loopback host, such as `127.0.0.1` or `localhost` (RFC 8252, section 7.3), and throws `ArgumentException` otherwise. 1.2.0 listened on any address it was given: a redirect on this machine's own network address or host name worked there and is now refused, and an address that is not this machine's failed with a `SocketException`.
- `StandaloneBrowser` listens on the redirect URL's host and port, and answers the browser off the main thread, so the page also loads while the application does not run in the background.

### Fixed
- `StandaloneBrowser` closes its listener once. 1.2.0 stopped it and then disposed it, which on Mono threw a `SocketException` ("Only one usage of each socket address") after a successful redirect while the browser still held its connection.
- `StandaloneBrowser` no longer throws when a second request reaches it after the redirect.
- `StandaloneBrowser` answers with `Connection: close`. On Mono, closing the listener wrote a second, unasked response (200, empty) onto a connection the browser had kept open.
- A cancelled `StandaloneBrowser` no longer opens the page when the token is already cancelled; cancelling still ends in `TaskCanceledException`.
- `ASWebAuthenticationSessionBrowser` no longer throws from the session's completion handler when the session answers after a timeout or a cancel (in an IL2CPP build an exception there ends the application). Each call has its own result.
- iOS: the sign-in sheet is anchored on Unity's own window, which the UIScene lifecycle requires; the app delegate's window is the fallback.

## [1.2.0] - 2023-07-02
### Changes
- Added Authorization Code flow with PKCE (Proof Key for Code Exchange) `AuthorizationCodeFlowWithPkce`.

## [1.1.3] - 2023-02-26
### Changes
- Json serializable classes marked with `[Preserve]` attribute to prevent them from being stripped out (i.e. UWP platform)

## [1.1.2] - 2023-02-1
### Changes
- Redirect URL can be used without forward slash with `StandaloneBrowser`
- Added `DeepLinkBrowser` through a custom scheme (aka protocol) for Android, iOS, or UWP

## [1.1.1] - 2022-12-16
### Changes
- Added `csc.rsp` to fix `error CS0103: The name 'HttpUtility' does not exist in the current context` error

## [1.1.0] - 2022-10-04
### Changes
- Added `MockServer` to be able to test app against a mock oauth provider
- Fixed access token expiration comparison

## [1.0.2] - 2022-09-28
### Changes
- Bug fixes

## [1.0.1] - 2022-09-27
### Changes
- Added Facebook auth

## [1.0.0] - 2022-09-27
### Changes
- Initial release
