using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cdm.Authentication.Browser
{
    public class ASWebAuthenticationSessionBrowser : IBrowser
    {
        /// <summary>
        /// Indicates whether the session should ask the browser for a private authentication
        /// session.
        /// </summary>
        /// <remarks>
        /// Set this property before you call <see cref="StartAsync"/>. Otherwise it has no effect.
        /// </remarks>
        public bool prefersEphemeralWebBrowserSession { get; set; } = false;
        
        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(loginUrl))
                throw new ArgumentNullException(nameof(loginUrl));
            
            if (string.IsNullOrEmpty(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));
            
            if (cancellationToken.IsCancellationRequested)
                return await Task.FromCanceled<BrowserResult>(cancellationToken);

            // One result per call: a session that answers late cannot complete a later call's result. The awaiting
            // code resumes outside the native completion handler, never inline inside it.
            var completion =
                new TaskCompletionSource<BrowserResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Discard URL parameters. They are not valid for iOS URL Scheme.
            redirectUrl = redirectUrl.Split(new char[] {':'}, StringSplitOptions.RemoveEmptyEntries)[0];

            using var authenticationSession = new ASWebAuthenticationSession(loginUrl, redirectUrl,
                (callbackUrl, error) => Complete(completion, callbackUrl, error));
            authenticationSession.prefersEphemeralWebBrowserSession = prefersEphemeralWebBrowserSession;

            using var registration = cancellationToken.Register(() => completion.TrySetCanceled());

            try
            {
                if (!authenticationSession.Start())
                {
                    completion.TrySetResult(
                        new BrowserResult(BrowserStatus.UnknownError, "Browser could not be started."));
                }

                return await completion.Task;
            }
            catch (TaskCanceledException)
            {
                // In case of timeout cancellation. The session then reports CanceledLogin to a result that is
                // already cancelled, which Complete ignores.
                authenticationSession?.Cancel();
                throw;
            }
        }

        // Called from native code: TrySetResult, because a session that answers after a timeout or a cancel must
        // not throw here. An exception escaping a native callback ends an IL2CPP app.
        private static void Complete(TaskCompletionSource<BrowserResult> completion, string callbackUrl,
            ASWebAuthenticationSessionError error)
        {
            if (error.code == ASWebAuthenticationSessionErrorCode.None)
            {
                completion.TrySetResult(
                    new BrowserResult(BrowserStatus.Success, callbackUrl));
            }
            else if (error.code == ASWebAuthenticationSessionErrorCode.CanceledLogin)
            {
                completion.TrySetResult(
                    new BrowserResult(BrowserStatus.UserCanceled, callbackUrl, error.message));
            }
            else
            {
                completion.TrySetResult(
                    new BrowserResult(BrowserStatus.UnknownError, callbackUrl, error.message));
            }
        }
    }
}