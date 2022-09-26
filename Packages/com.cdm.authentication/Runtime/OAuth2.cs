using System;
using System.Net;
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

        /// <summary>
        /// The access token returned by provider.
        /// </summary>
        public string accessToken { get; private set; }

        /// <summary>
        /// The refresh token returned by provider.
        /// </summary>
        public string refreshToken { get; set; }

        /// <summary>
        /// The token type returned by provider.
        /// </summary>
        public string tokenType { get; private set; }

        /// <summary>
        /// A space-separated list of scopes that you want the user to consent to.
        /// </summary>
        /// <seealso cref="AuthorizationRequest.scope"/>
        public string scope { get; private set; }

        /// <summary>
        /// Seconds till the <see cref="accessToken"/> expires returned by provider.
        /// </summary>
        public DateTime? expiresAt { get; private set; }
        
        /// <summary>
        /// The date and time that this token was issued, expressed in UTC.
        /// </summary>
        public DateTime? issuedAt { get; private set; }

        /// <summary>
        /// The lifetime in seconds of the access token.
        /// </summary>
        public long? expiresIn { get; private set; }

        public OAuth2Configuration configuration { get; }

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
        /// <returns><c>true</c> if a new authorization code is needed to be able to get access token.</returns>
        public bool ShouldRequestAuthorizationCode()
        {
            return string.IsNullOrEmpty(refreshToken) && ShouldRefreshAccessToken();
        }

        /// <summary>
        ///  Determines the need for retrieval of a new access token using the refresh token.
        /// </summary>
        /// <remarks>If <see cref="refreshToken"/> does not exist, then get new authorization code first.</remarks>
        /// <returns></returns>
        /// <seealso cref="ShouldRequestAuthorizationCode"/>
        public bool ShouldRefreshAccessToken()
        {
            return (expiresAt > DateTime.UtcNow || string.IsNullOrEmpty(accessToken));
        }

        public virtual Task<string> GetAuthorizationUrlAsync(CancellationToken cancellationToken = default)
        {
            // Generate new state.
            state = Guid.NewGuid().ToString("D");

            var parameters = JsonHelper.ToDictionary(new AuthorizationRequest()
            {
                responseType = "code",
                clientId = configuration.clientId,
                redirectUri = configuration.redirectUri,
                scope = configuration.scope,
                state = state
            });

            var url = UrlBuilder.New(authorizationUrl).SetQueryParameters(parameters).ToString();
            return Task.FromResult(url);
        }

        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="AccessTokenException"></exception>
        public virtual async Task<string> GetAccessTokenAsync(string authRedirectUrl,
            CancellationToken cancellationToken = default)
        {
            var authorizationResponseUri = new Uri(authRedirectUrl);
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

        public AuthenticationHeaderValue GetAuthenticationHeader()
        {
            return new AuthenticationHeaderValue(tokenType, accessToken);
        }

        /// <summary>
        /// Gets the access token immediately from cache if exist; or refreshes it and returns using the refresh token
        /// if available. 
        /// </summary>
        /// <param name="forceRefresh">Refreshes the access token using <see cref="refreshToken"/> if
        /// it is <c>true</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="AccessTokenException">If access token cannot be granted.</exception>
        public virtual async Task<string> GetAccessTokenAsync(bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            if (!forceRefresh && !ShouldRefreshAccessToken())
            {
                return accessToken;
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new AccessTokenException(new AccessTokenError()
                {
                    code = AccessTokenErrorCode.InvalidGrant,
                    description = "Refresh token does not exist."
                }, HttpStatusCode.Unauthorized);
            }
            
            var parameters = JsonHelper.ToDictionary(new RefreshTokenRequest()
            {
                refreshToken = refreshToken,
                scope = configuration.scope
            });

            Debug.Assert(parameters != null);

            return await GetAccessTokenInternalAsync(new FormUrlEncodedContent(parameters), cancellationToken);
        }

        private async Task<string> GetAccessTokenInternalAsync(FormUrlEncodedContent content,
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

                var accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseJson);

                accessToken = accessTokenResponse.accessToken;

                if (!string.IsNullOrEmpty(accessTokenResponse.refreshToken))
                {
                    refreshToken = accessTokenResponse.refreshToken;
                }

                tokenType = accessTokenResponse.tokenType;
                expiresIn = accessTokenResponse.expiresIn;

                if (expiresIn.HasValue)
                {
                    issuedAt = DateTime.UtcNow;
                    expiresAt = issuedAt + TimeSpan.FromSeconds(expiresIn.Value);
                }
                else
                {
                    issuedAt = null;
                    expiresAt = null;
                }
                
                scope = accessTokenResponse.scope;
                return accessToken;
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