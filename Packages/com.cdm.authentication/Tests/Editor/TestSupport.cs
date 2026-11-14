using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cdm.Authentication.OAuth2;
using NUnit.Framework;
using UnityEngine;

namespace Cdm.Authentication.Tests
{
    /// <summary>
    /// An authorization code flow against endpoints that are never reached by the tests that use it: the token
    /// endpoint is a loopback port nothing listens on, so a request that gets that far fails to connect.
    /// </summary>
    internal sealed class TestFlow : AuthorizationCodeFlow
    {
        public const string RedirectUri = "http://127.0.0.1:5000/callback";

        private readonly string _accessTokenUrl;

        private TestFlow(string accessTokenUrl, string redirectUri) : base(new Configuration
        {
            clientId = "test-client",
            redirectUri = redirectUri,
            scope = "openid"
        })
        {
            _accessTokenUrl = accessTokenUrl;
        }

        public override string authorizationUrl => "https://authorization.example.invalid/authorize";
        public override string accessTokenUrl => _accessTokenUrl;

        public static TestFlow WithUnreachableTokenEndpoint(string redirectUri = RedirectUri)
        {
            return new TestFlow($"http://127.0.0.1:{TestNet.UnusedLoopbackPort()}/token", redirectUri);
        }
    }

    internal static class TestNet
    {
        /// <summary>A loopback port that was free a moment ago; nothing listens on it afterwards.</summary>
        public static int UnusedLoopbackPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>Whether something listens on the loopback port.</summary>
        public static bool IsListening(int port)
        {
            try
            {
                var probe = new TcpListener(IPAddress.Loopback, port);
                probe.Start();
                probe.Stop();
                return false;
            }
            catch (SocketException)
            {
                return true;
            }
        }
    }

    internal static class TestTasks
    {
        /// <summary>Runs <paramref name="action"/> and returns what awaiting it throws, or null.</summary>
        public static async Task<Exception> Capture(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        /// <summary>Yields editor frames until the task completes, so its continuations run on the main thread.</summary>
        public static IEnumerator Wait(Task task, float timeoutSeconds = 15f)
        {
            var start = Time.realtimeSinceStartup;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup - start > timeoutSeconds)
                    Assert.Fail($"The task did not complete within {timeoutSeconds} s.");

                yield return null;
            }
        }
    }
}
