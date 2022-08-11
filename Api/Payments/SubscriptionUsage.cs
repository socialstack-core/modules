using Api.Users;
using System;

namespace Api.Payments
{

    /// <summary>
    /// A SubscriptionUsage
    /// </summary>
    public partial class SubscriptionUsage : UserCreatedContent<uint>
    {
        /// <summary>
        /// the product that this is a usage for.
        /// </summary>
        public uint ProductId;

        /// <summary>
        /// The id of the Subscription
        /// </summary>
        public uint SubscriptionId;

        /// <summary>
        /// The maximum usage of the subscription today
        /// </summary>
        public uint MaximumUsageToday;

        /// <summary>
		/// The timeslot index that this subscription was last charged. Usually in months. 
		/// Null indicates it has not been charged yet.
		/// This is defined as "The number of months that have occurred since 2020".
		/// </summary>
		public uint ChargedTimeslotId;

        /// <summary>
        /// The complete date that the usage represents. Not necessarily the same as the created date as some 
        /// subscription usage around midnight will be created the following day.
        /// </summary>
        public DateTime DateUtc;

    }

}