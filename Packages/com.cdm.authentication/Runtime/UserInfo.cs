namespace Cdm.Authorization
{
    /// <summary>
    /// Contains information about user who is being authenticated.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UserInfo()
        {
            avatarUrl = new AvatarInfo();
        }

        /// <summary>
        /// Unique identifier.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Friendly name of <see cref="UserInfo"/> provider (which is, in its turn, the client of OAuth/OAuth2 provider).
        /// </summary>
        /// <remarks>
        /// Supposed to be unique per OAuth/OAuth2 client.
        /// </remarks>
        public string providerName { get; set; }

        /// <summary>
        /// Email address.
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// First name.
        /// </summary>
        public string firstName { get; set; }

        /// <summary>
        /// Last name.
        /// </summary>
        public string lastName { get; set; }

        /// <summary>
        /// Photo URI.
        /// </summary>
        public string photoUri => avatarUrl.normal;

        /// <summary>
        /// Contains URIs of different sizes of avatar.
        /// </summary>
        public AvatarInfo avatarUrl { get; private set; }
    }
    
    public class AvatarInfo
    {
        /// <summary>
        /// Image size constants.
        /// </summary>
        internal const int SmallSize = 36;
        internal const int LargeSize = 300;

        /// <summary>
        /// Uri of small photo.
        /// </summary>
        public string small { get; set; }

        /// <summary>
        /// Uri of normal photo.
        /// </summary>
        public string normal { get; set; }

        /// <summary>
        /// Uri of large photo.
        /// </summary>
        public string large { get; set; }
    }
}