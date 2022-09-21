using System.Threading;
using System.Threading.Tasks;

namespace Cdm.Authorization
{
    public interface IUserInfoProvider
    {
        string userInfoUrl { get; }
        
        /// <summary>
        /// Obtains user information using third-party authentication service using data provided via callback request.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task<UserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default);
    }
}