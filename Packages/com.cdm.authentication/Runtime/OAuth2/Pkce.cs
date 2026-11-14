using System;
using System.Security.Cryptography;
using System.Text;

namespace Cdm.Authentication.OAuth2
{
    /// <summary>
    /// Proof Key for Code Exchange values (RFC 7636), for clients that run the authorization code flow themselves,
    /// for example because their token request is not the standard OAuth 2.0 one.
    /// <see cref="AuthorizationCodeFlowWithPkce"/> uses the same values.
    /// </summary>
    public static class Pkce
    {
        /// <summary>The challenge method of every challenge made here.</summary>
        public const string ChallengeMethod = "S256";

        /// <summary>A new code verifier: 32 random bytes, base64url without padding, 43 characters.</summary>
        public static string CreateCodeVerifier()
        {
            return RandomBase64Url(32);
        }

        /// <summary>The S256 challenge of <paramref name="codeVerifier"/>: base64url(SHA-256(ASCII(verifier))).</summary>
        public static string CreateCodeChallenge(string codeVerifier)
        {
            if (string.IsNullOrEmpty(codeVerifier))
                throw new ArgumentNullException(nameof(codeVerifier));

            using (var sha256 = SHA256.Create())
            {
                return Base64UrlEncodeNoPadding(sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier)));
            }
        }

        /// <summary>A new random state value: 16 random bytes, base64url without padding.</summary>
        public static string CreateState()
        {
            return RandomBase64Url(16);
        }

        private static string RandomBase64Url(int length)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Base64UrlEncodeNoPadding(bytes);
        }

        internal static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            return Convert.ToBase64String(buffer).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
