using System;
using System.Collections;
using System.Threading;
using Cdm.Authentication.Browser;
using Cdm.Authentication.OAuth2;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Cdm.Authentication.Tests
{
    /// <summary>
    /// <see cref="AuthenticationSession"/> tells a timeout from a cancel by the exception the browser ends with
    /// (<see cref="System.Threading.Tasks.TaskCanceledException"/>); the loopback browser keeps ending with it.
    /// </summary>
    public class AuthenticationSessionTests
    {
        private static (AuthenticationSession session, StandaloneBrowser browser) NewSession(TimeSpan loginTimeout)
        {
            var port = StandaloneBrowser.GetAvailableLoopbackPort();
            var flow = TestFlow.WithUnreachableTokenEndpoint($"http://127.0.0.1:{port}/callback");
            var browser = new StandaloneBrowser { openUrl = _ => { } };
            return (new AuthenticationSession(flow, browser) { loginTimeout = loginTimeout }, browser);
        }

        [UnityTest]
        public IEnumerator LoginTimeout_IsReportedAsTimeout()
        {
            var (session, _) = NewSession(TimeSpan.FromSeconds(1));
            using (session)
            {
                var authenticate = TestTasks.Capture(() => session.AuthenticateAsync());
                yield return TestTasks.Wait(authenticate);

                Assert.That(authenticate.Result, Is.TypeOf<AuthenticationException>(), authenticate.Result?.ToString());
                Assert.That(((AuthenticationException)authenticate.Result).error, Is.EqualTo(AuthenticationError.Timeout));
            }
        }

        [UnityTest]
        public IEnumerator CallerCancel_IsReportedAsCancelled()
        {
            var (session, _) = NewSession(TimeSpan.FromMinutes(10));
            using (session)
            using (var cancellation = new CancellationTokenSource())
            {
                var authenticate = TestTasks.Capture(() => session.AuthenticateAsync(cancellation.Token));
                cancellation.CancelAfter(TimeSpan.FromMilliseconds(300));
                yield return TestTasks.Wait(authenticate);

                Assert.That(authenticate.Result, Is.TypeOf<AuthenticationException>(), authenticate.Result?.ToString());
                Assert.That(((AuthenticationException)authenticate.Result).error,
                    Is.EqualTo(AuthenticationError.Cancelled));
            }
        }
    }
}
