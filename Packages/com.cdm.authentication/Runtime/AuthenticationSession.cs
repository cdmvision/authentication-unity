using System;
using System.Threading;
using System.Threading.Tasks;
using Cdm.Authentication.Browser;
using UnityEngine;

namespace Cdm.Authorization
{
    public class AuthenticationSession : IDisposable
    {
        public OAuth2 client { get; }
        private readonly IBrowser _browser;

        public TimeSpan timeout { get; set; } = TimeSpan.FromMinutes(10);

        public AuthenticationSession(OAuth2 client, IBrowser browser)
        {
            this.client = client;
            _browser = browser;
        }

        public bool ShouldAuthenticate()
        {
            return client.ShouldRequestAuthorizationCode();
        }

        /// <summary>
        /// Asynchronously authorizes the installed application to access user's protected data.
        /// </summary>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="AccessTokenException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        public async Task<string> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Open browser to start over authentication.
            try
            {
                // 1. Make authorization request.
                Debug.Log("Making authorization request...");

                var redirectUrl = client.configuration.redirectUri;
                var authorizationUrl = await client.GetAuthorizationUrlAsync(linkedCts.Token);

                // 2. Get authorization grant using login form in the browser.
                Debug.Log("Getting authorization grant using browser login...");

                var browserResult = await _browser.StartAsync(authorizationUrl, redirectUrl, linkedCts.Token);
                if (browserResult.status == BrowserStatus.Success)
                {
                    // 3. Exchange authorization code for access and refresh tokens.
                    Debug.Log("Exchanging authorization code for access and refresh tokens...");
                    
#if UNITY_EDITOR
                    Debug.Log($"Redirect URL: {browserResult.redirectUrl}");
#endif
                    return await client.GetAccessTokenAsync(browserResult.redirectUrl, linkedCts.Token);
                }

                if (browserResult.status == BrowserStatus.UserCanceled)
                {
                    throw new AuthenticationException(AuthenticationError.Cancelled, browserResult.error);
                }

                throw new AuthenticationException(AuthenticationError.Other, browserResult.error);
            }
            catch (TaskCanceledException e)
            {
                if (timeoutCts.IsCancellationRequested)
                    throw new AuthenticationException(AuthenticationError.Timeout, "Operation timed out.");

                throw new AuthenticationException(AuthenticationError.Cancelled, "Operation was cancelled.", e);
            }
        }

        /// <inheritdoc cref="OAuth2.GetAccessTokenAsync(bool,System.Threading.CancellationToken)"/>
        public async Task<string> GetAccessTokenAsync(bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            return await client.GetAccessTokenAsync(forceRefresh, cancellationToken);
        }
        
        /// <summary>
        /// Gets the access token by refreshing it with the refresh token given.
        /// </summary>
        /// <param name="refreshToken">The refresh token is used to refresh the access token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The access token.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="refreshToken"/> is <c>null</c> or empty.</exception>
        public async Task<string> GetAccessTokenAsync(string refreshToken, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            client.refreshToken = refreshToken;
            return await GetAccessTokenAsync(cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}