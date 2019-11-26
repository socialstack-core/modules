using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.AvailableEndpoints
{
	/// <summary>
	/// An available endpoint of the API.
	/// </summary>
	public class Endpoint
	{
		/// <summary>
		/// The URL to the endpoint including any substitute fields, e.g. "v1/user/{id}".
		/// </summary>
		public string Url;
		/// <summary>
		/// The summary of this endpoint.
		/// </summary>
		public string Summary;
		/// <summary>
		/// The fields which are subsituted into the URL.
		/// </summary>
		public Dictionary<string, object> UrlFields;
		/// <summary>
		/// The accepted fields to post to this endpoint.
		/// </summary>
		public Dictionary<string, object> BodyFields;
		/// <summary>
		/// The uppercase HTTP method for this endpoint.
		/// </summary>
		public string HttpMethod;

	}
}
