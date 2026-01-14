
using System;
using Newtonsoft.Json;

namespace Api.Users
{
    public partial class User
    {
        /// <summary>
        /// The User's current password, which is required to update a sensitive field. 
        /// </summary>
        [JsonIgnore]
        public string SensitiveFieldPassword { get; set; }

        /// <summary>
        /// The recovery key provided in the email, used to circumvent the sensitive field
        /// change when setting the new email and password for account recovery.
        /// </summary>
        [JsonIgnore]
        public string EmailRecovery { get; set; }

        /// <summary>
		/// Last time any sensitive fields on the account were changed. This is used to prevent further requests for a certain amount of time.
		/// </summary>
		[JsonIgnore]
		public DateTime? LastSensitiveFieldChangeUtc;
    }
}
