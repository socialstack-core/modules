using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Emails
{
	/// <summary>
	/// Used when creating an email template
	/// </summary>
	public partial class EmailTemplateCreate
	{
		/// <summary>
		/// The name of this template.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Admin notes on the template.
		/// </summary>
		[JsonProperty("notes")]
		public string Notes { get; set; }

		/// <summary>
		/// The canvas body JSON of the template. This also outputs the email subject too (as the document title).
		/// </summary>
		[JsonProperty("bodyJson")]
		public string BodyJson { get; set; }
	}
}
