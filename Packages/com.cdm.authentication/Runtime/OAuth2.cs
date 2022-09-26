using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Cdm.Authorization.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace Cdm.Authorization
{
    /// <summary>
    /// Supports 'Authorization Code' flow. Enables user sign-in and access to web APIs on behalf of the user.
    ///
    /// The OAuth 2.0 authorization code grant type, enables a client application to obtain
    /// authorized access to protected resources like web APIs. The auth code flow requires a user-agent that supports
    /// redirection from the authorization server back to your application. For example, a web browser, desktop,
    /// or mobile application operated by a user to sign in to your app and access their data.
    /// </summary>
    public abstract class OAuth2 : IDisposable
    {
        /// <summary>
        /// The endpoint for authorization server. This is used to get the authorization code.
        /// </summary>
        public abstract string authorizationUrl { get; }

        /// <summary>
        /// The endpoint for authentication server. This is used to exchange the authorization code for an access token.
        /// </summary>
        public abstract string accessTokenUrl { get; }

        /// <summary>
        /// The state; any additional information that was provided by application and is posted back by service.
        /// </summary>
        /// <seealso cref="AuthorizationRequest.state"/>
        public string state { get; private set; }

        public OAuth2Configuration configuration { get; }

        protected AccessTokenResponse accessTokenResponse { get; private set; }
        protected HttpClient httpClient { get; }

        protected OAuth2(OAuth2Configuration configuration)
        {
            this.configuration = configuration;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };
        }

        /// <summary>
        /// Determines the need for retrieval of a new authorization code.
        /// </summary>
        /// <returns>Indicates if a new authorization code needs to be retrieved.</returns>
        public bool ShouldRequestAuthorizationCode()
        {
            return accessTokenResponse == null || !accessTokenResponse.HasRefreshToken();
        }

        /// <summary>
        ///  Determines the need for retrieval of a new access token using the refresh token.
        /// </summary>
        /// <remarks>
        /// If <see cref="accessTokenResponse"/> does not exist, then get new authorization code first.
        /// </remarks>
        /// <returns>Indicates if a new access token needs to be retrieved.</returns>
        /// <seealso cref="ShouldRequestAuthorizationCode"/>
        public bool ShouldRefreshToken()
        {
            return accessTokenResponse.IsNullOrExpired();
        }

        /// <summary>,
        /// Gets an authorization code request URI with the specified <see cref="configuration"/>.
        /// </summary>
        /// <returns>The authorization code request URI.</returns>
        public string GetAuthorizationUrl()
        {
            // Generate new state.
            state = Guid.NewGuid().ToString("D");

            var parameters = JsonHelper.ToDictionary(new AuthorizationRequest()
            {
                clientId = configuration.clientId,
                redirectUri = configuration.redirectUri,
                scope = configuration.scope,
                state = state
            });

            return UrlBuilder.New(authorizationUrl).SetQueryParameters(parameters).ToString();
        }

        /// <summary>
        /// Asynchronously exchanges code with a token.
        /// </summary>
        /// <param name="redirectUrl">
        /// <see cref="Cdm.Authentication.Browser.BrowserResult.redirectUrl">Redirect URL</see> which is retrieved
        /// from the browser result.
        /// </param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>Access token response which contains the access token.</returns>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="Exception"></exception>
        /// <exception cref="SecurityException"></exception>
        /// <exception cref="AccessTokenException"></exception>
        public virtual async Task<AccessTokenResponse> ExchangeCodeForAccessTokenAsync(string redirectUrl,
            CancellationToken cancellationToken = default)
        {
            var authorizationResponseUri = new Uri(redirectUrl);
            var query = HttpUtility.ParseQueryString(authorizationResponseUri.Query);

            // Is there any error?
            if (JsonHelper.TryGetFromNameValueCollection<AuthorizationError>(query, out var authorizationError))
                throw new AuthorizationException(authorizationError);

            if (!JsonHelper.TryGetFromNameValueCollection<AuthorizationResponse>(query, out var authorizationResponse))
                throw new Exception("Authorization code could not get.");

            // Validate authorization response state.
            if (!string.IsNullOrEmpty(state) && state != authorizationResponse.state)
                throw new SecurityException($"Invalid state got: {authorizationResponse.state}");

            var parameters = JsonHelper.ToDictionary(new AccessTokenRequest()
            {
                code = authorizationResponse.code,
                clientId = configuration.clientId,
                clientSecret = configuration.clientSecret,
                redirectUri = configuration.redirectUri
            });

            Debug.Assert(parameters != null);

            return await GetAccessTokenInternalAsync(new FormUrlEncodedContent(parameters), cancellationToken);
        }

        /// <summary>
        /// Gets the access token immediately from cache if <see cref="ShouldRefreshToken"/> is <c>false</c>;
        /// or refreshes and returns it using the refresh token.
        /// if available. 
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <exception cref="AccessTokenException">If access token cannot be granted.</exception>
        public async Task<AccessTokenResponse> GetOrRefreshTokenAsync(
            CancellationToken cancellationToken = default)
        {
            if (ShouldRefreshToken())
            {
                return await RefreshTokenAsync(cancellationToken);
            }

            // Return from the cache immediately.
            return accessTokenResponse;
        }

        /// <summary>
        /// Asynchronously refreshes an access token using the refresh token from the <see cref="accessTokenResponse"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>Token response which contains the access token and the refresh token.</returns>
        public async Task<AccessTokenResponse> RefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            if (accessTokenResponse == null)
                throw new Exception("There is not authorization!");

            return await RefreshTokenAsync(accessTokenResponse.refreshToken, cancellationToken);
        }

        /// <summary>
        /// Asynchronously refreshes an access token using a refresh token.
        /// </summary>
        /// <param name="refreshToken">Refresh token which is used to get a new access token.</param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>Token response which contains the access token and the input refresh token.</returns>
        public async Task<AccessTokenResponse> RefreshTokenAsync(string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                var error = new AccessTokenError()
                {
                    code = AccessTokenErrorCode.InvalidGrant,
                    description = "Refresh token does not exist."
                };

                throw new AccessTokenException(error, null);
            }

            var parameters = JsonHelper.ToDictionary(new RefreshTokenRequest()
            {
                refreshToken = refreshToken,
                scope = configuration.scope
            });

            Debug.Assert(parameters != null);

            return await GetAccessTokenInternalAsync(new FormUrlEncodedContent(parameters), cancellationToken);
        }

        private async Task<AccessTokenResponse> GetAccessTokenInternalAsync(FormUrlEncodedContent content,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(content != null);

            var authString = $"{configuration.clientId}:{configuration.clientSecret}";
            var base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, accessTokenUrl);
            tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64AuthString);
            tokenRequest.Content = content;

#if UNITY_EDITOR
            Debug.Log($"{tokenRequest}");
            Debug.Log($"{await tokenRequest.Content.ReadAsStringAsync()}");
#endif

            var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();

#if UNITY_EDITOR
                Debug.Log(responseJson);
#endif

                accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseJson);
                accessTokenResponse.issuedAt = DateTime.UtcNow;

                return accessTokenResponse;
            }

            AccessTokenError error = null;
            try
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                error = JsonConvert.DeserializeObject<AccessTokenError>(errorJson);
            }
            catch (Exception)
            {
                // ignored
            }

            throw new AccessTokenException(error, response.StatusCode);
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}