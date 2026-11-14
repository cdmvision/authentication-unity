using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cdm.Authentication.Browser;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Cdm.Authentication.Tests
{
    /// <summary>
    /// What can run outside iOS: the session itself exists only there (its wrapper throws
    /// <see cref="NotImplementedException"/> anywhere else), so these cover the paths that end before it is made.
    /// </summary>
    public class ASWebAuthenticationSessionBrowserTests
    {
        [UnityTest]
        public IEnumerator AlreadyCancelled_EndsInTaskCanceledException_WithoutStartingASession()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var start = TestTasks.Capture(() => new ASWebAuthenticationSessionBrowser()
                .StartAsync("https://login.example.invalid/", "com.example.app:/callback", cancellation.Token));
            yield return TestTasks.Wait(start);

            Assert.That(start.Result, Is.InstanceOf<TaskCanceledException>());
        }

        [Test]
        public void MissingUrls_AreRefused()
        {
            var browser = new ASWebAuthenticationSessionBrowser();

            Assert.ThrowsAsync<ArgumentNullException>(() => browser.StartAsync(null, "com.example.app:/callback"));
            Assert.ThrowsAsync<ArgumentNullException>(() => browser.StartAsync("https://login.example.invalid/", ""));
        }
    }
}
