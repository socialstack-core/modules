using Api.Configuration;

namespace Api.Users
{
    /// <summary>
    /// Configurations used by the User Service.
    /// </summary>
    public class UserServiceConfig
    {
        /// <summary>
        /// Determines if user emails need to be unique.
        /// </summary>
        public bool UniqueEmails { get; set; } = false;

        /// <summary>
        /// Determines if user usernames need to be unique.
        /// </summary>
        public bool UniqueUsernames { get; set; } = false;
		
        /// <summary>
        /// Email validation required yes/no.
        /// </summary>
        public bool VerifyEmails { get; set; } = false;
    }
}
