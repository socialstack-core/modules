using System;
using Api.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.SmsMessages
{
	/// <summary>
	/// The config block for SMS.
	/// </summary>
    public class SmsConfig : Config
    {
		/// <summary>
		///  Scope for multiple sending addresses.
		/// </summary>
		public Dictionary<string, SmsAccount> Accounts { get; set; } = new Dictionary<string, SmsAccount>() {
			{ "default", new SmsAccount(){ } }
		};
    }

	/// <summary>
	/// A specific SMS sender.
	/// </summary>
    public class SmsAccount
    {
		/// <summary>
		/// The SID for Twilio
		/// </summary>
        public string TwilioSid { get; set; }

		/// <summary>
		/// The auth token for Twilio
		/// </summary>
        public string TwilioAuthToken { get; set; }

		/// <summary>
		/// The from phone number
		/// </summary>
        public string PhoneNumber { get; set; }
    }

}
