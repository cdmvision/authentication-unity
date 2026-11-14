using System;
using Cdm.Authentication.OAuth2;
using NUnit.Framework;

namespace Cdm.Authentication.Tests
{
    public class AuthorizationRedirectTests
    {
        [Test]
        public void LoopbackRedirect_IsRead()
        {
            var redirect = AuthorizationRedirect.Parse("http://127.0.0.1:51234/callback?code=the-code&state=the-state");

            Assert.That(redirect.code, Is.EqualTo("the-code"));
            Assert.That(redirect.state, Is.EqualTo("the-state"));
            Assert.That(redirect.error, Is.Null);
            Assert.That(redirect.errorDescription, Is.Null);
        }

        [Test]
        public void AppSchemeRedirect_IsRead()
        {
            // As ASWebAuthenticationSession returns it: the app's own scheme, no authority.
            var redirect = AuthorizationRedirect.Parse("com.example.app:/oauth/callback?code=the-code&state=the-state");

            Assert.That(redirect.code, Is.EqualTo("the-code"));
            Assert.That(redirect.state, Is.EqualTo("the-state"));
        }

        [Test]
        public void AppSchemeRedirect_WithDottedSchemeAndError_IsRead()
        {
            var redirect = AuthorizationRedirect.Parse("com.example.app.beta:/oauth/callback?error=access_denied&state=s");

            Assert.That(redirect.error, Is.EqualTo("access_denied"));
            Assert.That(redirect.code, Is.Null);
            Assert.That(redirect.state, Is.EqualTo("s"));
        }

        [Test]
        public void ErrorOutsideOAuth_IsReadAsItIs()
        {
            var redirect = AuthorizationRedirect.Parse(
                "http://127.0.0.1:51234/callback?error=account_suspended&error_description=Ask%20an%20admin&state=s");

            Assert.That(redirect.error, Is.EqualTo("account_suspended"));
            Assert.That(redirect.errorDescription, Is.EqualTo("Ask an admin"));
        }

        [Test]
        public void EncodedValues_AreDecoded()
        {
            var redirect = AuthorizationRedirect.Parse("com.example.app:/cb?code=a%2Bb%2Fc%3D&state=x%20y");

            Assert.That(redirect.code, Is.EqualTo("a+b/c="));
            Assert.That(redirect.state, Is.EqualTo("x y"));
        }

        [Test]
        public void Fragment_IsNotPartOfTheQuery()
        {
            var redirect = AuthorizationRedirect.Parse("http://127.0.0.1:51234/callback?code=c&state=s#code=other");

            Assert.That(redirect.code, Is.EqualTo("c"));
        }

        [Test]
        public void NoQuery_ReadsAsNothing()
        {
            var redirect = AuthorizationRedirect.Parse("com.example.app:/oauth/callback");

            Assert.That(redirect.code, Is.Null);
            Assert.That(redirect.state, Is.Null);
            Assert.That(redirect.error, Is.Null);
        }

        [Test]
        public void HasState_ComparesExactly()
        {
            var redirect = AuthorizationRedirect.Parse("com.example.app:/cb?code=c&state=AbC");

            Assert.That(redirect.HasState("AbC"), Is.True);
            Assert.That(redirect.HasState("abc"), Is.False);
            Assert.That(redirect.HasState("AbC "), Is.False);
        }

        [Test]
        public void HasState_NeverMatchesNothing()
        {
            var withoutState = AuthorizationRedirect.Parse("com.example.app:/cb?code=c");
            var emptyState = AuthorizationRedirect.Parse("com.example.app:/cb?code=c&state=");

            Assert.That(withoutState.HasState(null), Is.False);
            Assert.That(withoutState.HasState(""), Is.False);
            Assert.That(emptyState.HasState(""), Is.False);
        }

        [Test]
        public void NotAUrl_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AuthorizationRedirect.Parse(null));
            Assert.Throws<UriFormatException>(() => AuthorizationRedirect.Parse("not a url"));
            Assert.Throws<UriFormatException>(() => AuthorizationRedirect.Parse("/callback?code=c"));
        }
    }
}
