using Api.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Payments
{
    /// <summary>
	/// Configuration for Subscriptions
    /// </summary>
    public class SubscriptionConfig : Config
    {
        /// <summary>
		/// True if a thank you email should be sent.
		/// </summary>
        public bool SendThankYouEmail { get; set; } = false;
		
        /// <summary>
		/// True if an email should be sent before a renewal is about to happen.
		/// </summary>
        public bool SendRenewalEmails { get; set; } = false;
		
        /// <summary>
		/// True if a reminder to subscribe should be sent after this many days. If it's 0, it is not sent.
		/// </summary>
        public int SendSubscriptionReminderAfterDays { get; set; } = 0;
    }
}
