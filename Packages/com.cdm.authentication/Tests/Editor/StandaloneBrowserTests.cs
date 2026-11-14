using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cdm.Authentication.Browser;
using Cdm.Authentication.OAuth2;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Cdm.Authentication.Tests
{
    /// <summary>The loopback browser end to end, with an <see cref="HttpClient"/> in the place of the browser.</summary>
    public class StandaloneBrowserTests
    {
        private const string ClosePage = "<html><body>closing page</body></html>";

        private int _port;
        private string _redirectUrl;
        private string _openedUrl;
        private StandaloneBrowser _browser;
        private HttpClient _client;

        [SetUp]
        public void SetUp()
        {
            _port = StandaloneBrowser.GetAvailableLoopbackPort();
            _redirectUrl = $"http://127.0.0.1:{_port}/callback";
            _openedUrl = null;
            _browser = new StandaloneBrowser { closePageResponse = ClosePage, openUrl = url => _openedUrl = url };
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }

        private static IEnumerator Send(Task<HttpResponseMessage> request, Action<HttpResponseMessage> check)
        {
            yield return TestTasks.Wait(request);
            Assert.That(request.IsFaulted, Is.False, request.Exception?.ToString());
            check(request.Result);
        }

        [UnityTest]
        public IEnumerator Redirect_EndsTheWait_WithTheClosePage()
        {
            var start = _browser.StartAsync("https://login.example.invalid/", _redirectUrl);
            Assert.That(_openedUrl, Is.EqualTo("https://login.example.invalid/"));

            yield return Send(_client.GetAsync($"{_redirectUrl}?code=the-code&state=the-state"), response =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.Content.ReadAsStringAsync().Result, Is.EqualTo(ClosePage));
            });
            yield return TestTasks.Wait(start);

            Assert.That(start.Result.status, Is.EqualTo(BrowserStatus.Success));
            var redirect = AuthorizationRedirect.Parse(start.Result.redirectUrl);
            Assert.That(redirect.code, Is.EqualTo("the-code"));
            Assert.That(redirect.state, Is.EqualTo("the-state"));
            Assert.That(TestNet.IsListening(_port), Is.False, "the listener is closed after the redirect");
        }

        [UnityTest]
        public IEnumerator Redirect_IsTheOnlyAnswer_OnAConnectionTheBrowserWouldKeep()
        {
            var start = _browser.StartAsync("https://login.example.invalid/", _redirectUrl);

            // A browser asks to keep the connection; closing the listener must not answer on it a second time.
            var raw = Task.Run(() =>
            {
                using var socket = new TcpClient("127.0.0.1", _port);
                using var stream = socket.GetStream();
                stream.ReadTimeout = 3000;
                var request = Encoding.ASCII.GetBytes(
                    $"GET /callback?code=c&state=s HTTP/1.1\r\nHost: 127.0.0.1:{_port}\r\nConnection: keep-alive\r\n\r\n");
                stream.Write(request, 0, request.Length);

                var received = new MemoryStream();
                var buffer = new byte[4096];
                try
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        received.Write(buffer, 0, read);
                }
                catch (IOException)
                {
                    // Timed out with the connection still open: what was sent is all there is.
                }

                return Encoding.UTF8.GetString(received.ToArray());
            });

            yield return TestTasks.Wait(start);
            yield return TestTasks.Wait(raw);

            var answer = raw.Result;
            Assert.That(answer, Does.StartWith("HTTP/1.1 200"));
            Assert.That(answer.Split(new[] { "HTTP/1.1 " }, StringSplitOptions.None).Length - 1, Is.EqualTo(1),
                "one answer: " + answer);
            Assert.That(answer, Does.Contain("Connection: close"));
            Assert.That(answer, Does.EndWith(ClosePage));
        }

        [UnityTest]
        public IEnumerator EncodedValues_ReachTheReaderDecodedOnce()
        {
            // The listener's URL is what AuthorizationRedirect reads on the desktop.
            var start = _browser.StartAsync("https://login.example.invalid/", _redirectUrl);

            yield return Send(_client.GetAsync($"{_redirectUrl}?code=a%2Bb%2Fc%3D&state=x%20y%26z"), _ => { });
            yield return TestTasks.Wait(start);

            var redirect = AuthorizationRedirect.Parse(start.Result.redirectUrl);
            Assert.That(redirect.code, Is.EqualTo("a+b/c="));
            Assert.That(redirect.state, Is.EqualTo("x y&z"));
        }

        [UnityTest]
        public IEnumerator OtherPath_Gets404_AndTheWaitGoesOn()
        {
            var start = _browser.StartAsync("https://login.example.invalid/", _redirectUrl);

            yield return Send(_client.GetAsync($"http://127.0.0.1:{_port}/favicon.ico"),
                response => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound)));
            yield return Send(_client.GetAsync($"http://127.0.0.1:{_port}/callbackx?code=c"),
                response => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound)));
            Assert.That(start.IsCompleted, Is.False);

            yield return Send(_client.GetAsync($"{_redirectUrl}?code=c&state=s"),
                response => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)));
            yield return TestTasks.Wait(start);
            Assert.That(start.Result.status, Is.EqualTo(BrowserStatus.Success));
        }

        [UnityTest]
        public IEnumerator Post_Gets404_AndTheWaitGoesOn()
        {
            var start = _browser.StartAsync("https://login.example.invalid/", _redirectUrl);

            yield return Send(_client.PostAsync($"{_redirectUrl}?code=c&state=s", new StringContent("x")),
                response => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound)));
            Assert.That(start.IsCompleted, Is.False);

            yield return Send(_client.GetAsync($"{_redirectUrl}/?code=c&state=s"),
                response => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)));
            yield return TestTasks.Wait(start);
            Assert.That(start.Result.status, Is.EqualTo(BrowserStatus.Success));
        }

        [UnityTest]
        public IEnumerator ClosePage_CarriesNothingFromTheRequest()
        {
            var start = _browser.StartAsync("https://login.example.invalid/", _redirectUrl);

            yield return Send(_client.GetAsync($"{_redirectUrl}?error=%3Cscript%3Ex%3C%2Fscript%3E&state=s"),
                response => Assert.That(response.Content.ReadAsStringAsync().Result, Is.EqualTo(ClosePage)));
            yield return TestTasks.Wait(start);
        }

        [UnityTest]
        public IEnumerator Cancel_EndsInTaskCanceledException_AndFreesThePort()
        {
            using var cancellation = new CancellationTokenSource();
            var start = TestTasks.Capture(() =>
                _browser.StartAsync("https://login.example.invalid/", _redirectUrl, cancellation.Token));
            Assert.That(TestNet.IsListening(_port), Is.True, "listening while it waits");

            cancellation.Cancel();
            yield return TestTasks.Wait(start);

            Assert.That(start.Result, Is.InstanceOf<TaskCanceledException>());
            Assert.That(TestNet.IsListening(_port), Is.False, "the listener is closed after a cancel");
        }

        [UnityTest]
        public IEnumerator AlreadyCancelled_DoesNotOpenThePage()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var start = TestTasks.Capture(() =>
                _browser.StartAsync("https://login.example.invalid/", _redirectUrl, cancellation.Token));
            yield return TestTasks.Wait(start);

            Assert.That(start.Result, Is.InstanceOf<TaskCanceledException>());
            Assert.That(_openedUrl, Is.Null);
            Assert.That(TestNet.IsListening(_port), Is.False);
        }

        [UnityTest]
        public IEnumerator RequestAfterTheRedirect_DoesNotThrow()
        {
            var start = _browser.StartAsync("https://login.example.invalid/", _redirectUrl);

            yield return Send(_client.GetAsync($"{_redirectUrl}?code=c&state=s"), _ => { });
            yield return TestTasks.Wait(start);

            // Nobody listens any more: the client is refused, and nothing throws on the app's side (an exception
            // logged there would fail this test).
            var late = TestTasks.Capture(() => _client.GetAsync($"{_redirectUrl}?code=c&state=s"));
            yield return TestTasks.Wait(late);
            Assert.That(late.Result, Is.InstanceOf<HttpRequestException>());
            Assert.That(start.Result.status, Is.EqualTo(BrowserStatus.Success));
        }

        [Test]
        public void RedirectNotHttpOnLoopback_IsRefused()
        {
            Assert.ThrowsAsync<ArgumentException>(() =>
                _browser.StartAsync("https://login.example.invalid/", "http://example.com:5000/callback"));
            Assert.ThrowsAsync<ArgumentException>(() =>
                _browser.StartAsync("https://login.example.invalid/", $"https://127.0.0.1:{_port}/callback"));
            Assert.ThrowsAsync<ArgumentException>(() =>
                _browser.StartAsync("https://login.example.invalid/", "com.example.app:/callback"));
            Assert.That(_openedUrl, Is.Null);
        }

        [Test]
        public void LocalhostRedirect_IsLoopback()
        {
            Assert.That(new Uri($"http://localhost:{_port}/callback").IsLoopback, Is.True);
        }
    }
}
