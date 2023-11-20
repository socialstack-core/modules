
using System;
using System.Text.Json.Serialization;

namespace Api.Users
{
    public partial class User
    {
        /// <summary>
        /// The token provided in the password reset email. Useful for checking whether
        /// a user update event was triggered by a password reset request.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string PasswordReset { get; set; }
    }
}
