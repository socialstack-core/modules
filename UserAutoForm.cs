using Api.AutoForms;
using Newtonsoft.Json;


namespace Api.Users
{
    /// <summary>
    /// Used when creating or updating a user
    /// </summary>
    public partial class UserAutoForm : AutoForm<User>
    {
        /// <summary>
        /// The user's email address
        /// </summary>
        public string Email;

        /// <summary>
        /// The user's username.
        /// </summary>
        public string Username;

        /// <summary>
        /// The json that contains a user bio
        /// </summary>
        public string BioJson;
		
        /// <summary>
        /// A user's avatar.
        /// </summary>
        public string AvatarRef;
    }
}
