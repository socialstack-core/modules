using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.AvailableEndpoints
{
	/// <summary>
	/// Defines what's available from this API
	/// </summary>
	public class ApiStructure
	{
		/// <summary>
		/// The endpoints in this API.
		/// </summary>
		public List<Endpoint> Endpoints;
		/// <summary>
		/// The content types in this API.
		/// </summary>
		public List<ContentType> ContentTypes;
	}
}
