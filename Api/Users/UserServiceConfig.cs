using Api.Configuration;
using Api.Translate;

namespace Api.Users
{
    /// <summary>
    /// Configurations used by the User Service.
    /// </summary>
    public class UserServiceConfig : Config
    {
        /// <summary>
        /// True if default user (admin account) should be installed when it doesn't exist. This is only checked at startup.
        /// </summary>
        public bool InstallDefaultUser { get; set; } = true;

        /// <summary>
        /// Determines if user emails need to be unique.
        /// </summary>
        public bool UniqueEmails { get; set; } = false;

        /// <summary>
        /// True if a welcome email should be sent. Note that if verify emails is turned on, this is ignored.
        /// </summary>
        public bool SendWelcomeEmail { get; set; } = false;

        /// <summary>
        /// Determines if user usernames need to be unique.
        /// </summary>
        public bool UniqueUsernames { get; set; } = false;
		
        /// <summary>
        /// Email validation required yes/no.
        /// </summary>
        public bool VerifyEmails { get; set; } = false;

        /// <summary>
        /// Message which appears when performing unique email check. Note that this text will also pass through the locale system.
        /// </summary>
        public string UniqueEmailMessage { get; set; } = "This email is already in use.";

        /// <summary>
        /// Message which appears when performing unique username check. Note that this text will also pass through the locale system.
        /// </summary>
        public string UniqueUsernameMessage { get; set; } = "This username is already in use.";

        /// <summary>
        ///  If this is set to true, a verification email will not be sent to the user on creation.
        /// </summary>
        public bool NoVerificationEmail { get; set; } = false;
    }
}
