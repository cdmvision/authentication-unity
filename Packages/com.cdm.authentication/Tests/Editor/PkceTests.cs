using System;
using System.Text.RegularExpressions;
using Cdm.Authentication.OAuth2;
using NUnit.Framework;

namespace Cdm.Authentication.Tests
{
    public class PkceTests
    {
        private static readonly Regex Base64Url = new Regex("^[A-Za-z0-9_-]+$");

        [Test]
        public void Challenge_MatchesRfc7636AppendixB()
        {
            Assert.That(Pkce.CreateCodeChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"),
                Is.EqualTo("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"));
        }

        [Test]
        public void Verifier_Is43Base64UrlCharacters()
        {
            var verifier = Pkce.CreateCodeVerifier();

            Assert.That(verifier.Length, Is.EqualTo(43));
            Assert.That(Base64Url.IsMatch(verifier), Is.True, verifier);
        }

        [Test]
        public void State_Is22Base64UrlCharacters()
        {
            var state = Pkce.CreateState();

            Assert.That(state.Length, Is.EqualTo(22));
            Assert.That(Base64Url.IsMatch(state), Is.True, state);
        }

        [Test]
        public void Values_AreNotRepeated()
        {
            Assert.That(Pkce.CreateCodeVerifier(), Is.Not.EqualTo(Pkce.CreateCodeVerifier()));
            Assert.That(Pkce.CreateState(), Is.Not.EqualTo(Pkce.CreateState()));
        }

        [Test]
        public void Challenge_OfNothing_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Pkce.CreateCodeChallenge(null));
            Assert.Throws<ArgumentNullException>(() => Pkce.CreateCodeChallenge(""));
        }

        [Test]
        public void ChallengeMethod_IsS256()
        {
            Assert.That(Pkce.ChallengeMethod, Is.EqualTo("S256"));
        }
    }
}
