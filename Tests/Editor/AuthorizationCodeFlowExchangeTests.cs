using System;
using System.Collections;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Cdm.Authentication.OAuth2;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Cdm.Authentication.Tests
{
    /// <summary>
    /// What <see cref="AuthorizationCodeFlow.ExchangeCodeForAccessTokenAsync"/> throws for each kind of redirect, as
    /// 1.2.0 does it. Callers outside this package rely on these types and on their order, so they stay as they are.
    /// </summary>
    public class AuthorizationCodeFlowExchangeTests
    {
        private static IEnumerator Exchange(TestFlow flow, string redirectUrl, Action<Exception> check)
        {
            var task = TestTasks.Capture(() => flow.ExchangeCodeForAccessTokenAsync(redirectUrl));
            yield return TestTasks.Wait(task);
            check(task.Result);
        }

        [UnityTest]
        public IEnumerator ErrorRedirect_ThrowsAuthorizationCodeRequestException()
        {
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, $"{TestFlow.RedirectUri}?error=access_denied&state={flow.state}", e =>
            {
                Assert.That(e, Is.TypeOf<AuthorizationCodeRequestException>());
                Assert.That(((AuthorizationCodeRequestException)e).error.code,
                    Is.EqualTo(AuthorizationCodeRequestErrorCode.AccessDenied));
            });
        }

        [UnityTest]
        public IEnumerator ErrorRedirect_IsReportedBeforeAWrongState()
        {
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, $"{TestFlow.RedirectUri}?error=access_denied&state=not-the-state",
                e => Assert.That(e, Is.TypeOf<AuthorizationCodeRequestException>()));
        }

        [UnityTest]
        public IEnumerator ErrorCodeOutsideOAuth_ThrowsPlainException()
        {
            // An error code the enum does not know does not read as an error: with no code either, the redirect
            // fails as one without a code.
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, $"{TestFlow.RedirectUri}?error=account_suspended&state={flow.state}",
                e => Assert.That(e, Is.TypeOf<Exception>()));
        }

        [UnityTest]
        public IEnumerator NoCode_ThrowsPlainException_BeforeAWrongState()
        {
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, $"{TestFlow.RedirectUri}?state=not-the-state",
                e => Assert.That(e, Is.TypeOf<Exception>()));
        }

        [UnityTest]
        public IEnumerator WrongState_ThrowsSecurityException()
        {
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, $"{TestFlow.RedirectUri}?code=the-code&state=not-the-state",
                e => Assert.That(e, Is.TypeOf<SecurityException>()));
        }

        [UnityTest]
        public IEnumerator MissingState_ThrowsSecurityException_WhenOneWasSent()
        {
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, $"{TestFlow.RedirectUri}?code=the-code",
                e => Assert.That(e, Is.TypeOf<SecurityException>()));
        }

        [UnityTest]
        public IEnumerator MatchingState_GoesOnToTheTokenRequest()
        {
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, $"{TestFlow.RedirectUri}?code=the-code&state={flow.state}",
                e => Assert.That(e, Is.InstanceOf<HttpRequestException>()));
        }

        [UnityTest]
        public IEnumerator NoStateSent_SkipsTheStateCheck()
        {
            // GetAuthorizationUrl was never called, so the flow has no state to compare with.
            using var flow = TestFlow.WithUnreachableTokenEndpoint();

            yield return Exchange(flow, $"{TestFlow.RedirectUri}?code=the-code&state=anything",
                e => Assert.That(e, Is.InstanceOf<HttpRequestException>()));
        }

        [UnityTest]
        public IEnumerator InvalidUrl_ThrowsUriFormatException()
        {
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, "not a url", e => Assert.That(e, Is.InstanceOf<UriFormatException>()));
        }

        [UnityTest]
        public IEnumerator CustomSchemeRedirect_IsRead()
        {
            // An app's own scheme without an authority, as ASWebAuthenticationSession returns it.
            using var flow = TestFlow.WithUnreachableTokenEndpoint();
            flow.GetAuthorizationUrl();

            yield return Exchange(flow, $"com.example.app:/oauth/callback?code=the-code&state={flow.state}",
                e => Assert.That(e, Is.InstanceOf<HttpRequestException>()));
        }
    }
}
