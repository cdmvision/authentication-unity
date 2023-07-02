using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Cdm.Authentication.OAuth2
{
    /// <summary>
    /// OAuth 2.0 'Authorization Code' flow with PCKE
    /// https://www.rfc-editor.org/rfc/rfc7636
    /// </summary>
    public abstract class AuthorizationCodeFlowWithPKCE : AuthorizationCodeFlow
    {
        private string codeVerifier;

        protected AuthorizationCodeFlowWithPKCE(Configuration configuration) : base(configuration)
        {
        }

        protected override Dictionary<string, string> GetAuthorizationUrlParameters()
        {
            var parameters = base.GetAuthorizationUrlParameters();

            codeVerifier = GenerateRandomDataBase64url(32);
            string codeChallenge = Base64UrlEncodeNoPadding(Sha256Ascii(codeVerifier));

            parameters.Add("code_challenge", codeChallenge);
            parameters.Add("code_challenge_method", "S256");

            return parameters;
        }

        protected override Dictionary<string, string> GetAccessTokenParameters(string code)
        {
            var parameters = base.GetAccessTokenParameters(code);
            parameters.Add("code_verifier", codeVerifier);
            return parameters;
        }

        private static string GenerateRandomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return Base64UrlEncodeNoPadding(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string, which is assumed to be ASCII.
        /// </summary>
        private static byte[] Sha256Ascii(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                return sha256.ComputeHash(bytes);
            }
        }
    }
}
