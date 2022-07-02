using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.AutoForms;
using Api.Database;
using Newtonsoft.Json;

namespace Api.TemporaryGuests
{
    
    /// <summary>
    /// Contains information about the temporary guest account
    /// </summary>
    public class TemporaryGuest : Content<uint>
    {
        /// <summary>
        ///the date at which this invite starts
        /// </summary>
        ///
        public DateTime StartsUtc;

        /// <summary>
        ///the date at which this invite expires
        /// </summary>
        public DateTime ExpiresUtc;

        /// <summary>
        /// The first name of the invited user
        /// </summary>
        public string FirstName;

        /// <summary>
        /// The last name of this user
        /// </summary>
        public string LastName;

        /// <summary>
        /// Email account of the user
        /// </summary>
        public string Email;

        /// <summary>
        /// salted hash for authentication
        /// </summary>
        [Module("Admin/TemporaryGuestLink")]
        [Data("readonly", "true")]
        public string Token;
        
    }
}
