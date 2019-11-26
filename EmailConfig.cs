using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Emails
{
	/// <summary>
	/// The appsettings.json config block for email config.
	/// </summary>
    public class EmailConfig
    {
        /// <summary>
        ///  Scope for multiple sending addresses.
        /// </summary>
        public Dictionary<string, EmailAccount> Accounts { get; set; }
    }

	/// <summary>
	/// A specific email account from the appsettings.json.
	/// </summary>
    public class EmailAccount
    {
		/// <summary>
		/// The servers DNS or IP address to send via.
		/// </summary>
        public string Server { get; set; }

		/// <summary>
		/// The address that emails using this account will be from.
		/// </summary>
        public string FromAddress { get; set; }

		/// <summary>
		/// The username to login to the server with (usually the same as the from address).
		/// </summary>
        public string User { get; set; }

		/// <summary>
		/// The password to login to the server with.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// The default address to use as the reply-to field when sending with this account.
		/// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// NOTE: Azure blocks port 25. Must issue a support request to unblock it. Use 465 instead.
        /// </summary>
        public int Port { get; set; }

		/// <summary>
		/// True if the email traffic is encrypted.
		/// </summary>
        public bool Encrypted { get; set; }
    }

}
