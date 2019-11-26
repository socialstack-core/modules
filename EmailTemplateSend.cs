using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Emails
{
	/// <summary>
	/// Used when sending test emails.
	/// </summary>
    public class EmailTemplateSend
    {
		/// <summary>
		/// The email address to send it to.
		/// </summary>
        [JsonProperty("address")]
        public string Address { get; set; }

		/// <summary>
		/// The name of the template to send.
		/// </summary>
        [JsonProperty("template")]
        public string Template { get; set; }
    }
}
