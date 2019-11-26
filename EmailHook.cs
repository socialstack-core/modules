using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Emails
{
    /// <summary>
    /// Used when the email hook fires
    /// </summary>
    public class EmailHook
    {
		/// <summary>
		/// The email address to send to.
		/// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
