using Api.Database;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.AvailableEndpoints
{
	/// <summary>
	/// Defines what's available from this API
	/// </summary>
	[CacheOnly]
	public class ApiStructure : Content<uint>
	{
		/// <summary>
		/// The endpoints in this API.
		/// </summary>
		public List<Endpoint> Endpoints { get; set; }
		/// <summary>
		/// The content types in this API.
		/// </summary>
		public List<ContentType> ContentTypes { get; set; }
	}
}
