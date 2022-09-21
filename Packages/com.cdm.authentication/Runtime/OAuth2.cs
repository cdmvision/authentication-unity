using System;
using System.Linq;
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
        /// State (any additional information that was provided by application and is posted back by service).
        /// </summary>
        public string state { get; private set; }

        /// <summary>
        /// The access token returned by provider.
        /// </summary>
        public string accessToken { get; private set; }

        /// <summary>
        /// The refresh token returned by provider.
        /// </summary>
        public string refreshToken { get; private set; }

        /// <summary>
        /// The token type returned by provider.
        /// </summary>
        public string tokenType { get; private set; }

        public string scope { get; private set; }
        
        /// <summary>
        /// Seconds till the <see cref="accessToken"/> expires returned by provider.
        /// </summary>
        public DateTime expiresAt { get; private set; }
        
        /// <summary>
        /// The lifetime in seconds of the access token.
        /// </summary>
        public int expiresIn { get; private set; }

        public OAuth2Configuration configuration { get; }
        
        private readonly HttpClient _httpClient;
        
        protected OAuth2(OAuth2Configuration configuration)
        {
            this.configuration = configuration;
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };
        }
        
        public virtual Task<string> GetAuthorizationUrlAsync(CancellationToken cancellationToken = default)
        {
            // Generate new state.
            state = Guid.NewGuid().ToString("D");

            var parameters = JsonHelper.ToDictionary(new AuthorizationRequest()
            {
                responseType = "code",
                clientId = configuration.clientId,
                clientSecret = configuration.clientSecret,
                redirectUri = configuration.redirectUri,
                scope = configuration.scope,
                state = state
            });
            
            var url = UrlBuilder.New(authorizationUrl).SetQueryParameters(parameters).ToString();
            return Task.FromResult(url);
        }

        public virtual async Task<string> GetAccessTokenAsync(string authorizationResponseString, 
            CancellationToken cancellationToken = default)
        {
            var index = authorizationResponseString.IndexOf("?", StringComparison.Ordinal);
            if (index >= 0)
            {
                authorizationResponseString = authorizationResponseString.Substring(index).Remove(0, 1);
            }
            
            var query = HttpUtility.ParseQueryString(authorizationResponseString);

            // Is there any error?
            if (JsonHelper.TryGetFromNameValueCollection<AuthorizationError>(query, out var authorizationError))
                throw new AuthorizationException(authorizationError);

            if (!JsonHelper.TryGetFromNameValueCollection<AuthorizationResponse>(query, out var authorizationResponse))
                throw new Exception("Authorization code could not get.");

            // Validate authorization response state.
            if (!string.IsNullOrEmpty(state) && state != authorizationResponse.state)
                throw new SecurityException($"Invalid state got: {authorizationResponse.state}");

            var authString = $"{configuration.clientId}:{configuration.clientSecret}";
            var base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
            
            var parameters = JsonHelper.ToDictionary(new AccessTokenRequest()
            {
                code = authorizationResponse.code,
                clientId = configuration.clientId,
                clientSecret = configuration.clientSecret,
                redirectUri = configuration.redirectUri
            });
            
            Debug.Assert(parameters != null);
            
            var accessTokenRequest = new HttpRequestMessage(HttpMethod.Post, accessTokenUrl);
            accessTokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64AuthString);
            accessTokenRequest.Content = new FormUrlEncodedContent(parameters);
            
            Debug.Log($"{accessTokenRequest}");
            Debug.Log($"{await accessTokenRequest.Content.ReadAsStringAsync()}");
            
            var response = await _httpClient.SendAsync(accessTokenRequest, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseJson);

                accessToken = accessTokenResponse.accessToken;
                refreshToken = accessTokenResponse.refreshToken;
                tokenType = accessTokenResponse.tokenType;
                expiresIn = accessTokenResponse.expiresIn;
                expiresAt = DateTime.Now + TimeSpan.FromSeconds(expiresIn);
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

       /* public virtual async Task<string> GetAccessTokenAsync(bool forceRefresh = false, 
            CancellationToken cancellationToken = default)
        {
            if (!forceRefresh && expiresAt > DateTime.Now && !string.IsNullOrEmpty(accessToken))
            {
                return accessToken;
            }
            
            
        }*/

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}