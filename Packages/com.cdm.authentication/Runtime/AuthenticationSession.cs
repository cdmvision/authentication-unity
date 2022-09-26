using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Cdm.Authentication.Browser;
using UnityEngine;

namespace Cdm.Authorization
{
    public class AuthenticationSession : IDisposable
    {
        private readonly OAuth2 _client;
        private readonly IBrowser _browser;

        public TimeSpan loginTimeout { get; set; } = TimeSpan.FromMinutes(10);

        public AuthenticationSession(OAuth2 client, IBrowser browser)
        {
            _client = client;
            _browser = browser;
        }

        public bool ShouldAuthenticate()
        {
            return _client.ShouldRequestAuthorizationCode();
        }

        public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync()
        {
            var tokenResponse = await _client.GetOrRefreshTokenAsync();
            return tokenResponse.GetAuthenticationHeader();
        }

        /// <summary>
        /// Asynchronously authorizes the installed application to access user's protected data.
        /// </summary>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="AccessTokenException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        public async Task<AccessTokenResponse> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource(loginTimeout);
            
            try
            {
                // 1. Create authorization request URL.
                Debug.Log("Making authorization request...");
                
                var redirectUrl = _client.configuration.redirectUri;
                var authorizationUrl = _client.GetAuthorizationUrl();

                // 2. Get authorization code grant using login form in the browser.
                Debug.Log("Getting authorization grant using browser login...");

                
                using var loginCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCancellationTokenSource.Token);
                
                var browserResult = 
                    await _browser.StartAsync(authorizationUrl, redirectUrl, loginCancellationTokenSource.Token);
                if (browserResult.status == BrowserStatus.Success)
                {
                    // 3. Exchange authorization code for access and refresh tokens.
                    Debug.Log("Exchanging authorization code for access and refresh tokens...");
                    
#if UNITY_EDITOR
                    Debug.Log($"Redirect URL: {browserResult.redirectUrl}");
#endif
                    return await _client.ExchangeCodeForAccessTokenAsync(browserResult.redirectUrl, cancellationToken);
                }

                if (browserResult.status == BrowserStatus.UserCanceled)
                {
                    throw new AuthenticationException(AuthenticationError.Cancelled, browserResult.error);
                }

                throw new AuthenticationException(AuthenticationError.Other, browserResult.error);
            }
            catch (TaskCanceledException e)
            {
                if (timeoutCancellationTokenSource.IsCancellationRequested)
                    throw new AuthenticationException(AuthenticationError.Timeout, "Operation timed out.");

                throw new AuthenticationException(AuthenticationError.Cancelled, "Operation was cancelled.", e);
            }
        }

        /// <inheritdoc cref="OAuth2.GetOrRefreshTokenAsync"/>
        public async Task<AccessTokenResponse> GetOrRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            return await _client.GetOrRefreshTokenAsync(cancellationToken);
        }

        /// <inheritdoc cref="OAuth2.RefreshTokenAsync(System.Threading.CancellationToken)"/>
        public async Task<AccessTokenResponse> RefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            return await _client.RefreshTokenAsync(cancellationToken);
        }

        /// <inheritdoc cref="OAuth2.RefreshTokenAsync(string,System.Threading.CancellationToken)"/>
        public async Task<AccessTokenResponse> RefreshTokenAsync(string refreshToken,
            CancellationToken cancellationToken = default)
        {
            return await _client.RefreshTokenAsync(refreshToken, cancellationToken);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}